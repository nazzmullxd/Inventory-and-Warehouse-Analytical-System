using Iwas.Model.Contracts.Common;
using Iwas.Model.Source;

namespace Iwas.Business.Stock;

public sealed record StockLayer(
    string MovementId,
    DateOnly ReceiptDate,
    int? Sequence,
    decimal RemainingQuantity,
    decimal? UnitPurchasePrice);

public sealed record StockPosition(
    string ItemId,
    DateOnly AsOfDate,
    decimal ReceivedQuantity,
    decimal IssuedQuantity,
    decimal ClosingQuantity,
    IReadOnlyList<StockLayer> RemainingLayers);

public sealed class StockLedgerService
{
    public AnalysisResult<StockPosition> Calculate(
        string itemId,
        DateOnly asOfDate,
        IEnumerable<StockMovementRecord> movements,
        bool historyCoverageComplete = true)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return Fail("validation_failed", "An item ID is required.", itemId);
        if (!historyCoverageComplete)
            return Fail("stock_history_insufficient", "Complete stock history is unavailable.", itemId);

        var relevant = movements.Where(x => IdEquals(x.ItemId, itemId) && x.MovementDate <= asOfDate).ToList();
        var duplicate = relevant.GroupBy(x => x.MovementId, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            return Fail("duplicate_source_key", $"Duplicate movement identity '{duplicate.Key}'.", itemId);

        if (relevant.Any(x => x.Quantity <= 0))
            return Fail("validation_failed", "Movement quantities must be positive.", itemId, "Quantity");

        var ambiguousDay = relevant.GroupBy(x => x.MovementDate)
            .FirstOrDefault(g => g.Count() > 1 && (g.Any(x => x.MovementSequence is null) || g.GroupBy(x => x.MovementSequence).Any(x => x.Count() > 1)));
        if (ambiguousDay is not null)
            return Fail("ambiguous_movement_order", $"Movement order is not proven for {ambiguousDay.Key:yyyy-MM-dd}.", itemId);

        var ordered = relevant.OrderBy(x => x.MovementDate).ThenBy(x => x.MovementSequence).ToList();
        var layers = new Queue<MutableLayer>();
        decimal received = 0, issued = 0;

        foreach (var movement in ordered)
        {
            if (movement.MovementType == StockMovementType.Receipt)
            {
                received += movement.Quantity;
                layers.Enqueue(new MutableLayer(movement, movement.Quantity));
                continue;
            }

            issued += movement.Quantity;
            var remainingIssue = movement.Quantity;
            while (remainingIssue > 0 && layers.Count > 0)
            {
                var layer = layers.Peek();
                var consumed = Math.Min(layer.Remaining, remainingIssue);
                layer.Remaining -= consumed;
                remainingIssue -= consumed;
                if (layer.Remaining == 0) layers.Dequeue();
            }

            if (remainingIssue > 0)
                return Fail("stock_history_insufficient", $"Movement '{movement.MovementId}' issues more stock than was available.", itemId);
        }

        var resultLayers = layers.Select(x => new StockLayer(
            x.Source.MovementId, x.Source.MovementDate, x.Source.MovementSequence,
            x.Remaining, x.Source.UnitPurchasePrice)).ToArray();
        var closing = resultLayers.Sum(x => x.RemainingQuantity);
        return AnalysisResult<StockPosition>.Success(new(itemId.Trim(), asOfDate, received, issued, closing, resultLayers));
    }

    private static bool IdEquals(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static AnalysisResult<StockPosition> Fail(string code, string message, string? id, string? field = null) =>
        AnalysisResult<StockPosition>.Failure(new AnalysisIssue(code, IssueSeverity.Blocking, message, id, field));

    private sealed class MutableLayer(StockMovementRecord source, decimal remaining)
    {
        public StockMovementRecord Source { get; } = source;
        public decimal Remaining { get; set; } = remaining;
    }
}
