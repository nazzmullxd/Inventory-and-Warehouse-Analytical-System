using Iwas.Business.Abstractions;
using Iwas.Business.Matching;
using Iwas.Business.Reorder;
using Iwas.Business.Stock;
using Iwas.Business.Suppliers;
using Iwas.Business.Valuation;
using Iwas.Model.Contracts.Common;
using Iwas.Model.Contracts.Queries;
using Iwas.Model.Repositories;
using Iwas.Model.Source;

namespace Iwas.Business.Dashboard;

public sealed class WarehouseAnalyticsService(
    IWarehouseReadStore store,
    StockLedgerService stock,
    FifoValuationService valuation,
    SupplierPerformanceService suppliers,
    ReorderService reorder,
    RequisitionMatchingService matching) : IWarehouseAnalytics
{
    public async Task<WarehouseAnalysis<StockValuation>> ValueItemAsync(string itemId, DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        await using var context = await store.OpenReadContextAsync(cancellationToken);
        var item = await context.GetItemAsync(itemId, cancellationToken);
        if (!item.IsComplete) return Fail<StockValuation>(context, item.Issues);
        var movements = await context.GetStockHistoryAsync(new([item.Data!.ItemId], asOfDate), cancellationToken);
        if (!movements.IsComplete) return Fail<StockValuation>(context, movements.Issues);
        var result = valuation.Value(item.Data, asOfDate, movements.Data!, HasCoverage(context.Metadata, asOfDate));
        return Wrap(context, result);
    }

    public async Task<WarehouseAnalysis<IReadOnlyList<StockValuation>>> ValueWarehouseAsync(DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        await using var context = await store.OpenReadContextAsync(cancellationToken);
        var items = await AllItems(context, cancellationToken);
        if (!items.IsComplete) return Fail<IReadOnlyList<StockValuation>>(context, items.Issues);
        var movements = await context.GetStockHistoryAsync(new(items.Data!.Select(x => x.ItemId).ToArray(), asOfDate), cancellationToken);
        if (!movements.IsComplete) return Fail<IReadOnlyList<StockValuation>>(context, movements.Issues);
        var values = new List<StockValuation>(); var issues = new List<AnalysisIssue>();
        foreach (var item in items.Data!)
        {
            var result = valuation.Value(item, asOfDate, movements.Data!, HasCoverage(context.Metadata, asOfDate));
            if (result.IsComplete) values.Add(result.Data!); else issues.AddRange(result.Issues);
        }
        return issues.Any(x => x.Severity == IssueSeverity.Blocking) ? new(null, context.Metadata, issues) : new(values, context.Metadata, issues);
    }

    public async Task<WarehouseAnalysis<IReadOnlyList<ReorderAnalysis>>> AnalyzeReorderAsync(DateOnly analysisDate, CancellationToken cancellationToken = default)
    {
        await using var context = await store.OpenReadContextAsync(cancellationToken);
        var items = await AllItems(context, cancellationToken);
        if (!items.IsComplete) return Fail<IReadOnlyList<ReorderAnalysis>>(context, items.Issues);
        var ids = items.Data!.Select(x => x.ItemId).ToArray();
        var movements = await context.GetStockHistoryAsync(new(ids, analysisDate), cancellationToken);
        var deliveries = await context.GetDeliveredOrdersAsync(new(new(analysisDate.AddDays(-364), analysisDate)), cancellationToken);
        if (!movements.IsComplete || !deliveries.IsComplete) return Fail<IReadOnlyList<ReorderAnalysis>>(context, movements.Issues.Concat(deliveries.Issues));
        var rows = new List<ReorderAnalysis>(); var issues = new List<AnalysisIssue>();
        foreach (var item in items.Data!)
        {
            var result = reorder.Analyze(item, analysisDate, movements.Data!, deliveries.Data!, demandHistoryComplete: HasDemandCoverage(context.Metadata, analysisDate), stockHistoryComplete: HasCoverage(context.Metadata, analysisDate));
            if (result.IsComplete) rows.Add(result.Data!); else issues.AddRange(result.Issues);
        }
        return issues.Any(x => x.Severity == IssueSeverity.Blocking) ? new(null, context.Metadata, issues) : new(rows, context.Metadata, issues);
    }

    public async Task<WarehouseAnalysis<IReadOnlyList<SupplierPerformance>>> AnalyzeSuppliersAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        await using var context = await store.OpenReadContextAsync(cancellationToken);
        var deliveries = await context.GetDeliveredOrdersAsync(new(new(start, end)), cancellationToken);
        var open = await context.GetOpenDeliveriesAsync(new(new(start, end)), cancellationToken);
        if (!deliveries.IsComplete || !open.IsComplete) return Fail<IReadOnlyList<SupplierPerformance>>(context, deliveries.Issues.Concat(open.Issues));
        var result = suppliers.Analyze(start, end, deliveries.Data!.Concat(open.Data!));
        return Wrap(context, result);
    }

    public async Task<WarehouseAnalysis<RequisitionMatch>> MatchRequisitionAsync(string requisitionId, DateOnly stockDate, CancellationToken cancellationToken = default)
    {
        await using var context = await store.OpenReadContextAsync(cancellationToken);
        var requisition = await context.GetRequisitionAsync(requisitionId, cancellationToken);
        if (!requisition.IsComplete) return Fail<RequisitionMatch>(context, requisition.Issues);
        var items = await AllItems(context, cancellationToken);
        if (!items.IsComplete) return Fail<RequisitionMatch>(context, items.Issues);
        var movements = await context.GetStockHistoryAsync(new(items.Data!.Select(x => x.ItemId).ToArray(), stockDate), cancellationToken);
        if (!movements.IsComplete) return Fail<RequisitionMatch>(context, movements.Issues);
        return Wrap(context, matching.Match(requisition.Data!, items.Data!, stockDate, movements.Data!, HasCoverage(context.Metadata, stockDate)));
    }

    public async Task<WarehouseAnalysis<IReadOnlyList<RequisitionMatch>>> MatchPeriodAsync(DateOnly start, DateOnly end, DateOnly stockDate, CancellationToken cancellationToken = default)
    {
        await using var context = await store.OpenReadContextAsync(cancellationToken);
        var requisitions = await context.GetRequisitionsAsync(new(new(start, end)), new(1, 500), cancellationToken);
        var items = await AllItems(context, cancellationToken);
        if (!requisitions.IsComplete || !items.IsComplete) return Fail<IReadOnlyList<RequisitionMatch>>(context, requisitions.Issues.Concat(items.Issues));
        var movements = await context.GetStockHistoryAsync(new(items.Data!.Select(x => x.ItemId).ToArray(), stockDate), cancellationToken);
        if (!movements.IsComplete) return Fail<IReadOnlyList<RequisitionMatch>>(context, movements.Issues);
        var results = requisitions.Data!.Rows.Select(x => matching.Match(x, items.Data!, stockDate, movements.Data!, HasCoverage(context.Metadata, stockDate))).ToArray();
        var issues = results.SelectMany(x => x.Issues).ToArray();
        return results.Any(x => !x.IsComplete) ? new(null, context.Metadata, issues) : new(results.Select(x => x.Data!).ToArray(), context.Metadata, issues);
    }

    public async Task<WarehouseAnalysis<WarehouseOverview>> BuildOverviewAsync(DateOnly analysisDate, CancellationToken cancellationToken = default)
    {
        var values = await ValueWarehouseAsync(analysisDate, cancellationToken);
        var reorders = await AnalyzeReorderAsync(analysisDate, cancellationToken);
        var supplierRows = await AnalyzeSuppliersAsync(analysisDate.AddDays(-364), analysisDate, cancellationToken);
        var matches = await MatchPeriodAsync(analysisDate, analysisDate, analysisDate, cancellationToken);
        var issues = values.Issues.Concat(reorders.Issues).Concat(supplierRows.Issues).Concat(matches.Issues).ToArray();
        var overview = new WarehouseOverview(analysisDate,
            values.IsComplete ? values.Data!.Sum(x => x.FifoValue) : null,
            reorders.Data?.Count(x => x.ReorderNow) ?? 0, reorders.Data?.Count(x => x.IsDeadStock) ?? 0,
            supplierRows.Data?.Count(x => x.Standing == SupplierStanding.WatchList) ?? 0,
            matches.Data?.Count(x => x.Status == MatchStatus.ClarificationRequired) ?? 0,
            reorders.Data ?? [], supplierRows.Data ?? [], matches.Data ?? []);
        return new(overview, values.Context, issues);
    }

    public async Task<WarehouseAnalysis<IReadOnlyList<DailyMovement>>> BuildDailyMovementAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        await using var context = await store.OpenReadContextAsync(cancellationToken);
        var items = await AllItems(context, cancellationToken);
        if (!items.IsComplete) return Fail<IReadOnlyList<DailyMovement>>(context, items.Issues);
        var movements = await context.GetStockHistoryAsync(new(items.Data!.Select(x => x.ItemId).ToArray(), date), cancellationToken);
        if (!movements.IsComplete) return Fail<IReadOnlyList<DailyMovement>>(context, movements.Issues);
        var rows = new List<DailyMovement>(); var issues = new List<AnalysisIssue>();
        foreach (var item in items.Data!)
        {
            var opening = stock.Calculate(item.ItemId, date.AddDays(-1), movements.Data!, HasCoverage(context.Metadata, date));
            var closing = stock.Calculate(item.ItemId, date, movements.Data!, HasCoverage(context.Metadata, date));
            if (!opening.IsComplete || !closing.IsComplete) { issues.AddRange(opening.Issues); issues.AddRange(closing.Issues); continue; }
            var today = movements.Data!.Where(x => string.Equals(x.ItemId, item.ItemId, StringComparison.OrdinalIgnoreCase) && x.MovementDate == date).ToArray();
            if (opening.Data!.ClosingQuantity != 0 || closing.Data!.ClosingQuantity != 0 || today.Length != 0)
                rows.Add(new(item.ItemId, item.Name, item.Unit, opening.Data.ClosingQuantity,
                    today.Where(x => x.MovementType == StockMovementType.Receipt).Sum(x => x.Quantity),
                    today.Where(x => x.MovementType == StockMovementType.Issue).Sum(x => x.Quantity), closing.Data!.ClosingQuantity,
                    today.Count(x => x.MovementType == StockMovementType.Receipt), today.Count(x => x.MovementType == StockMovementType.Issue)));
        }
        return issues.Any(x => x.Severity == IssueSeverity.Blocking) ? new(null, context.Metadata, issues) : new(rows, context.Metadata, issues);
    }

    private static async Task<QueryResult<IReadOnlyList<ItemRecord>>> AllItems(IWarehouseReadContext context, CancellationToken token)
    {
        var page = await context.SearchItemsAsync(new(), new(1, 500), token);
        return page.IsComplete ? QueryResult<IReadOnlyList<ItemRecord>>.Success(page.Data!.Rows, context.Metadata)
            : QueryResult<IReadOnlyList<ItemRecord>>.Failure(context.Metadata, page.Issues.ToArray());
    }
    private static bool HasCoverage(ReadContextMetadata metadata, DateOnly through) => metadata.HasTrustedOpeningStock && metadata.ReliableHistoryStart <= through;
    private static bool HasDemandCoverage(ReadContextMetadata metadata, DateOnly through) => metadata.ReliableHistoryStart <= through.AddDays(-364);
    private static WarehouseAnalysis<T> Wrap<T>(IWarehouseReadContext context, AnalysisResult<T> result) => new(result.Data, context.Metadata, result.Issues);
    private static WarehouseAnalysis<T> Fail<T>(IWarehouseReadContext context, IEnumerable<AnalysisIssue> issues) => new(default, context.Metadata, issues.ToArray());
}
