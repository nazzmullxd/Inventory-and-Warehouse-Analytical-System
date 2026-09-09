using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Iwas.Business.Stock;
using Iwas.Model.Contracts.Common;
using Iwas.Model.Source;

namespace Iwas.Business.Matching;

public enum MatchStatus { AutoMatched, ClarificationRequired }
public enum MatchReason { Qualified, BelowThreshold, AmbiguousTopScore, NoCandidates, EmptyRequisitionText }
public enum QuantityStatus { Parsed, Missing, Ambiguous, Unsupported }
public enum UnitCompatibility { Compatible, Incompatible, Unverified }

public sealed record QuantityInterpretation(decimal? Quantity, string? Unit, QuantityStatus Status);
public sealed record MatchCandidate(string ItemId, int CommonTerms, int RequestTerms, int ItemTerms, double Similarity, int Rank);
public sealed record RequisitionMatch(
    string RequisitionId, MatchStatus Status, MatchReason Reason, string? MatchedItemId,
    IReadOnlyList<MatchCandidate> Candidates, QuantityInterpretation Quantity,
    UnitCompatibility UnitCompatibility, decimal? UnitPrice, decimal? TotalPrice,
    decimal? StockOnHand, bool? HasSufficientStock,
    string? Department = null, string? SourceText = null, DateOnly? RequisitionDate = null);

public sealed class RequisitionMatchingService(StockLedgerService ledger)
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    { "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "in", "into", "is", "it", "of", "on", "or", "that", "the", "this", "to", "with" };

    private static readonly Dictionary<string, int> NumberWords = new(StringComparer.Ordinal)
    { ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5, ["six"] = 6,
      ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10, ["eleven"] = 11, ["twelve"] = 12 };

    private static readonly Dictionary<string, string> Units = new(StringComparer.OrdinalIgnoreCase)
    { ["piece"] = "each", ["pieces"] = "each", ["pc"] = "each", ["pcs"] = "each", ["each"] = "each",
      ["unit"] = "each", ["units"] = "each", ["ream"] = "ream", ["reams"] = "ream", ["box"] = "box", ["boxes"] = "box" };

    public AnalysisResult<RequisitionMatch> Match(
        RequisitionRecord requisition, IEnumerable<ItemRecord> catalogue, DateOnly stockDate,
        IEnumerable<StockMovementRecord>? stockMovements = null, bool stockHistoryComplete = true)
    {
        if (stockDate < requisition.RequisitionDate)
            return AnalysisResult<RequisitionMatch>.Failure(new AnalysisIssue("validation_failed", IssueSeverity.Blocking,
                "Stock date cannot be earlier than the requisition date.", requisition.RequisitionId, "StockDate"));

        var quantity = ParseQuantity(requisition.Description);
        var requestTerms = Normalize(RemoveQuantitySpans(requisition.Description));
        var items = catalogue.Where(x => !string.IsNullOrWhiteSpace(x.Description)).ToArray();
        if (requestTerms.Count == 0)
            return AnalysisResult<RequisitionMatch>.Success(new(requisition.RequisitionId,
                MatchStatus.ClarificationRequired, MatchReason.EmptyRequisitionText, null, [], quantity,
                UnitCompatibility.Unverified, null, null, null, null, requisition.Department, requisition.Description, requisition.RequisitionDate));
        if (items.Length == 0)
            return AnalysisResult<RequisitionMatch>.Success(new(requisition.RequisitionId,
                MatchStatus.ClarificationRequired, MatchReason.NoCandidates, null, [], quantity,
                UnitCompatibility.Unverified, null, null, null, null, requisition.Department, requisition.Description, requisition.RequisitionDate));

        var scored = items.Select(item =>
        {
            var terms = Normalize(item.Description);
            var common = requestTerms.Intersect(terms).Count();
            return new Score(item, common, requestTerms.Count, terms.Count,
                terms.Count == 0 ? 0 : common / Math.Sqrt((double)requestTerms.Count * terms.Count));
        }).OrderByDescending(x => x, ScoreComparer.Instance).ThenBy(x => x.Item.ItemId, StringComparer.OrdinalIgnoreCase).ToArray();
        var candidates = scored.Take(3).Select((x, i) => new MatchCandidate(x.Item.ItemId, x.Common, x.RequestCount, x.ItemCount, x.Similarity, i + 1)).ToArray();
        var top = scored[0];
        var qualifies = top.RequestCount > 0 && top.ItemCount > 0 && 25L * top.Common * top.Common >= 16L * top.RequestCount * top.ItemCount;
        var tied = scored.Length > 1 && ScoreComparer.Instance.Compare(top, scored[1]) == 0;
        if (!qualifies || tied)
            return AnalysisResult<RequisitionMatch>.Success(new(requisition.RequisitionId,
                MatchStatus.ClarificationRequired, tied && qualifies ? MatchReason.AmbiguousTopScore : MatchReason.BelowThreshold,
                null, candidates, quantity, UnitCompatibility.Unverified, null, null, null, null,
                requisition.Department, requisition.Description, requisition.RequisitionDate));

        var compatibility = DetermineCompatibility(quantity, top.Item.Unit);
        var safeQuantity = quantity.Status == QuantityStatus.Parsed && compatibility == UnitCompatibility.Compatible ? quantity.Quantity : null;
        var total = safeQuantity is not null && top.Item.CatalogueUnitPrice is not null ? safeQuantity * top.Item.CatalogueUnitPrice : null;
        decimal? stock = null;
        bool? sufficient = null;
        var issues = new List<AnalysisIssue>();
        if (stockMovements is not null)
        {
            var position = ledger.Calculate(top.Item.ItemId, stockDate, stockMovements, stockHistoryComplete);
            if (position.IsComplete)
            {
                stock = position.Data!.ClosingQuantity;
                sufficient = safeQuantity is null ? null : stock >= safeQuantity;
            }
            else issues.AddRange(position.Issues.Select(x => x with { Severity = IssueSeverity.Warning }));
        }
        if (top.Item.CatalogueUnitPrice is null)
            issues.Add(new("catalogue_price_unavailable", IssueSeverity.Informational, "Catalogue price is unavailable.", top.Item.ItemId));
        if (quantity.Status == QuantityStatus.Parsed && compatibility != UnitCompatibility.Compatible)
            issues.Add(new("quantity_unit_unverified", IssueSeverity.Warning, "Quantity cannot safely be converted to the item's base unit.", top.Item.ItemId));

        return new(new RequisitionMatch(requisition.RequisitionId, MatchStatus.AutoMatched, MatchReason.Qualified, top.Item.ItemId,
            candidates, quantity, compatibility, top.Item.CatalogueUnitPrice, total, stock, sufficient,
            requisition.Department, requisition.Description, requisition.RequisitionDate), issues);
    }

    public static HashSet<string> Normalize(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        var tokens = Regex.Matches(normalized, @"[\p{L}\p{Nd}]+")
            .Select(x => Singularize(x.Value)).Where(x => !StopWords.Contains(x));
        return new(tokens, StringComparer.Ordinal);
    }

    public static QuantityInterpretation ParseQuantity(string text)
    {
        var matches = Regex.Matches(text.ToLowerInvariant(), @"\b(?<n>\d+|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)\s*(?<dozen>dozen)?\s*(?<unit>pieces?|pcs?|each|units?|reams?|boxes?)?\b");
        if (matches.Count == 0) return new(null, null, QuantityStatus.Missing);
        if (matches.Count > 1) return new(null, null, QuantityStatus.Ambiguous);
        var match = matches[0];
        var raw = match.Groups["n"].Value;
        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) && !NumberWords.TryGetValue(raw, out amount))
            return new(null, null, QuantityStatus.Unsupported);
        if (match.Groups["dozen"].Success) amount *= 12;
        var unitText = match.Groups["unit"].Value;
        string? unit = unitText.Length == 0 ? match.Groups["dozen"].Success ? "each" : null : Units[unitText];
        return new(amount, unit, QuantityStatus.Parsed);
    }

    private static string RemoveQuantitySpans(string text) => Regex.Replace(text,
        @"\b(\d+|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)\s*(dozen)?\s*(pieces?|pcs?|each|units?|reams?|boxes?)?\b", " ", RegexOptions.IgnoreCase);
    private static UnitCompatibility DetermineCompatibility(QuantityInterpretation quantity, string itemUnit)
    {
        if (quantity.Status != QuantityStatus.Parsed || quantity.Unit is null) return UnitCompatibility.Unverified;
        var canonicalItem = Units.TryGetValue(itemUnit.Trim(), out var value) ? value : itemUnit.Trim().ToLowerInvariant();
        return quantity.Unit == canonicalItem ? UnitCompatibility.Compatible : UnitCompatibility.Incompatible;
    }
    private static string Singularize(string word)
    {
        if (word.Length > 4 && word.EndsWith("ies", StringComparison.Ordinal)) return word[..^3] + "y";
        if (word.Length > 4 && Regex.IsMatch(word, "(s|x|z|ch|sh)es$")) return word[..^2];
        if (word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss") && !word.EndsWith("us") && !word.EndsWith("is")) return word[..^1];
        return word;
    }

    private sealed record Score(ItemRecord Item, int Common, int RequestCount, int ItemCount, double Similarity);
    private sealed class ScoreComparer : IComparer<Score>
    {
        public static ScoreComparer Instance { get; } = new();
        public int Compare(Score? x, Score? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            return ((long)x.Common * x.Common * y.ItemCount).CompareTo((long)y.Common * y.Common * x.ItemCount);
        }
    }
}
