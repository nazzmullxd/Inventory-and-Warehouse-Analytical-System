using Iwas.Business.Stock;
using Iwas.Business.Suppliers;
using Iwas.Model.Contracts.Common;
using Iwas.Model.Source;

namespace Iwas.Business.Reorder;

public enum ReorderAction { ReviewNoDemand, ReviewDeadStock, ReviewOrderQuantity, OrderNow, OrderInDays, Ok }

public sealed record ReorderAnalysis(
    string ItemId, DateOnly AnalysisDate, DateOnly DemandWindowStart, DateOnly DeadStockWindowStart,
    decimal AnnualDemand, decimal DailyDemand, decimal StockOnHand, decimal RawEoq, decimal SuggestedEoq,
    decimal BaseLeadTimeDays, decimal SupplierDelayDays, decimal EffectiveLeadTimeDays,
    decimal LeadDemand, decimal SafetyStock, decimal ReorderPoint, bool ReorderNow, int? DaysToReorderPoint,
    DateOnly? LastIssueDate, bool IsDeadStock, bool IsDisposalCandidate, ReorderAction Action);

public sealed class ReorderService(StockLedgerService ledger, SupplierPerformanceService suppliers)
{
    public AnalysisResult<ReorderAnalysis> Analyze(
        ItemRecord item, DateOnly analysisDate, IEnumerable<StockMovementRecord> movements,
        IEnumerable<SupplierDeliveryRecord> deliveries, decimal baseLeadTimeDays = 5,
        bool demandHistoryComplete = true, bool stockHistoryComplete = true)
    {
        if (item.HoldingCostPerUnitPerYear <= 0)
            return Fail("holding_cost_invalid", "Annual holding cost must be greater than zero.", item.ItemId);
        if (item.OrderingCostPerPurchaseOrder < 0 || baseLeadTimeDays < 0)
            return Fail("validation_failed", "Ordering cost and base lead time cannot be negative.", item.ItemId);
        if (!demandHistoryComplete)
            return Fail("insufficient_demand_history", "The complete demand window is unavailable.", item.ItemId);

        var movementRows = movements.Where(x => string.Equals(x.ItemId.Trim(), item.ItemId.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
        var position = ledger.Calculate(item.ItemId, analysisDate, movementRows, stockHistoryComplete);
        if (!position.IsComplete) return new(null, position.Issues);
        var demandStart = analysisDate.AddDays(-364);
        var deadStart = analysisDate.AddDays(-179);
        var issues = movementRows.Where(x => x.MovementType == StockMovementType.Issue && x.MovementDate <= analysisDate).ToArray();
        var demand = issues.Where(x => x.MovementDate >= demandStart).Sum(x => x.Quantity);
        var lastIssue = issues.Select(x => (DateOnly?)x.MovementDate).Max();
        var isDead = !issues.Any(x => x.MovementDate >= deadStart);
        var delay = suppliers.ResolveItemDelay(item.ItemId, analysisDate, deliveries);
        if (!delay.IsComplete) return new(null, delay.Issues);

        var daily = demand / 365m;
        decimal rawEoq;
        try { rawEoq = DecimalSqrt(2m * demand * item.OrderingCostPerPurchaseOrder / item.HoldingCostPerUnitPerYear); }
        catch (OverflowException) { return Fail("numeric_overflow", "EOQ exceeds the supported numeric range.", item.ItemId); }
        var suggested = Math.Round(rawEoq, 0, MidpointRounding.AwayFromZero);
        if (rawEoq > 0 && suggested < 1) suggested = 1;
        var effectiveLead = baseLeadTimeDays + delay.Data!.DelayContributionDays;
        var leadDemand = effectiveLead * daily;
        var safety = .20m * leadDemand;
        var rop = leadDemand + safety;
        var reorderNow = position.Data!.ClosingQuantity <= rop;
        int? days = reorderNow ? 0 : daily > 0 ? checked((int)Math.Ceiling((position.Data.ClosingQuantity - rop) / daily)) : null;
        var action = demand == 0 ? ReorderAction.ReviewNoDemand
            : isDead && position.Data.ClosingQuantity > 0 ? ReorderAction.ReviewDeadStock
            : suggested == 0 ? ReorderAction.ReviewOrderQuantity
            : reorderNow ? ReorderAction.OrderNow
            : days is not null ? ReorderAction.OrderInDays : ReorderAction.Ok;
        var result = new ReorderAnalysis(item.ItemId, analysisDate, demandStart, deadStart, demand, daily,
            position.Data.ClosingQuantity, rawEoq, suggested, baseLeadTimeDays, delay.Data.DelayContributionDays,
            effectiveLead, leadDemand, safety, rop, reorderNow, days, lastIssue, isDead,
            isDead && position.Data.ClosingQuantity > 0, action);
        return new(result, delay.Issues);
    }

    private static decimal DecimalSqrt(decimal value)
    {
        if (value == 0) return 0;
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        var x = (decimal)Math.Sqrt((double)value);
        for (var i = 0; i < 16; i++) x = (x + value / x) / 2;
        return x;
    }

    private static AnalysisResult<ReorderAnalysis> Fail(string code, string message, string id) =>
        AnalysisResult<ReorderAnalysis>.Failure(new AnalysisIssue(code, IssueSeverity.Blocking, message, id));
}
