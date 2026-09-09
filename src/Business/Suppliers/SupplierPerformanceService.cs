using Iwas.Model.Contracts.Common;
using Iwas.Model.Source;

namespace Iwas.Business.Suppliers;

public enum SupplierStanding { WatchList, Good, Excellent }

public sealed record SupplierPerformance(
    string SupplierId, string SupplierName, DateOnly PeriodStart, DateOnly PeriodEnd,
    int DeliveredOrders, int OnTimeOrders, int LateOrders, decimal OnTimeRatio,
    decimal? AverageLateDelayDays, SupplierStanding Standing, int ExcludedOpenOrders);

public sealed record SupplierDelay(
    string ItemId, string? SupplierId, string? SupplierName, DateOnly WindowStart, DateOnly WindowEnd,
    int DeliveredOrders, decimal DelayContributionDays, string Provenance);

public sealed class SupplierPerformanceService
{
    public AnalysisResult<IReadOnlyList<SupplierPerformance>> Analyze(
        DateOnly start, DateOnly end, IEnumerable<SupplierDeliveryRecord> deliveries)
    {
        if (end < start)
            return AnalysisResult<IReadOnlyList<SupplierPerformance>>.Failure(new AnalysisIssue(
                "validation_failed", IssueSeverity.Blocking, "The period end must be on or after its start.", Field: "Period"));

        var source = deliveries.ToArray();
        var delivered = source.Where(x => x.ActualDeliveryDate >= start && x.ActualDeliveryDate <= end).ToArray();
        var duplicate = delivered.GroupBy(OrderIdentity, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            return AnalysisResult<IReadOnlyList<SupplierPerformance>>.Failure(new AnalysisIssue(
                "delivery_identity_ambiguous", IssueSeverity.Blocking, $"Purchase order '{duplicate.Key}' is represented more than once."));

        var result = delivered.GroupBy(x => x.SupplierId, StringComparer.OrdinalIgnoreCase).Select(group =>
        {
            var names = group.Select(x => x.SupplierName.Trim()).Distinct(StringComparer.Ordinal).ToArray();
            if (names.Length != 1) throw new InvalidDataException($"Supplier '{group.Key}' has conflicting names.");
            var rows = group.ToArray();
            var onTime = rows.Count(x => x.ActualDeliveryDate <= x.PromisedDeliveryDate);
            var lateDays = rows.Where(x => x.ActualDeliveryDate > x.PromisedDeliveryDate)
                .Select(x => x.ActualDeliveryDate!.Value.DayNumber - x.PromisedDeliveryDate.DayNumber).ToArray();
            var ratio = (decimal)onTime / rows.Length;
            var standing = ratio == 1 ? SupplierStanding.Excellent : ratio < .80m ? SupplierStanding.WatchList : SupplierStanding.Good;
            var open = source.Count(x => string.Equals(x.SupplierId, group.Key, StringComparison.OrdinalIgnoreCase)
                && x.ActualDeliveryDate is null && x.PromisedDeliveryDate >= start && x.PromisedDeliveryDate <= end);
            return new SupplierPerformance(group.Key, names[0], start, end, rows.Length, onTime,
                lateDays.Length, ratio, lateDays.Length == 0 ? null : (decimal)lateDays.Sum() / lateDays.Length, standing, open);
        }).OrderBy(x => x.SupplierId, StringComparer.OrdinalIgnoreCase).ToArray();

        return AnalysisResult<IReadOnlyList<SupplierPerformance>>.Success(result);
    }

    public AnalysisResult<SupplierDelay> ResolveItemDelay(
        string itemId, DateOnly analysisDate, IEnumerable<SupplierDeliveryRecord> deliveries)
    {
        var start = analysisDate.AddDays(-364);
        var rows = deliveries.Where(x => string.Equals(x.ItemId.Trim(), itemId.Trim(), StringComparison.OrdinalIgnoreCase)
            && x.ActualDeliveryDate >= start && x.ActualDeliveryDate <= analysisDate).ToArray();
        if (rows.Length == 0)
            return AnalysisResult<SupplierDelay>.Success(
                new(itemId, null, null, start, analysisDate, 0, 0, "no_supplier_history"),
                new AnalysisIssue("no_supplier_history", IssueSeverity.Warning, "No delivered supplier history was available; delay contribution is zero.", itemId));

        var selected = rows.GroupBy(x => x.SupplierId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Rows = g.ToArray(), Count = g.Select(OrderIdentity).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Latest = g.Max(x => x.ActualDeliveryDate) })
            .OrderByDescending(x => x.Count).ThenByDescending(x => x.Latest)
            .ThenBy(x => x.Rows[0].SupplierId, StringComparer.OrdinalIgnoreCase).First();
        var distinct = selected.Rows.GroupBy(OrderIdentity, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToArray();
        var late = distinct.Where(x => x.ActualDeliveryDate > x.PromisedDeliveryDate)
            .Select(x => x.ActualDeliveryDate!.Value.DayNumber - x.PromisedDeliveryDate.DayNumber).ToArray();
        return AnalysisResult<SupplierDelay>.Success(new(itemId, selected.Rows[0].SupplierId, selected.Rows[0].SupplierName,
            start, analysisDate, distinct.Length, late.Length == 0 ? 0 : (decimal)late.Sum() / late.Length, "item_specific_trailing_365_days"));
    }

    private static string OrderIdentity(SupplierDeliveryRecord row) =>
        string.IsNullOrWhiteSpace(row.PurchaseOrderId) ? row.DeliveryRecordKey : row.PurchaseOrderId;
}
