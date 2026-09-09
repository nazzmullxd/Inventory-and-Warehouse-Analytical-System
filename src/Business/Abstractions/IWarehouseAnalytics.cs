using Iwas.Business.Matching;
using Iwas.Business.Reorder;
using Iwas.Business.Suppliers;
using Iwas.Business.Valuation;
using Iwas.Model.Contracts.Common;

namespace Iwas.Business.Abstractions;

public sealed record WarehouseAnalysis<T>(T? Data, ReadContextMetadata Context, IReadOnlyList<AnalysisIssue> Issues)
{
    public bool IsComplete => Data is not null && Issues.All(x => x.Severity != IssueSeverity.Blocking);
}

public sealed record WarehouseOverview(
    DateOnly AnalysisDate,
    decimal? WarehouseFifoValue,
    int OrderNowCount,
    int DeadStockCount,
    int WatchListSupplierCount,
    int ClarificationCount,
    IReadOnlyList<ReorderAnalysis> Reorders,
    IReadOnlyList<SupplierPerformance> Suppliers,
    IReadOnlyList<RequisitionMatch> Matches);

public sealed record DailyMovement(
    string ItemId, string ItemName, string Unit, decimal OpeningQuantity,
    decimal ReceivedQuantity, decimal IssuedQuantity, decimal ClosingQuantity,
    int ReceiptMovementCount, int IssueMovementCount);

public interface IWarehouseAnalytics
{
    Task<WarehouseAnalysis<StockValuation>> ValueItemAsync(string itemId, DateOnly asOfDate, CancellationToken cancellationToken = default);
    Task<WarehouseAnalysis<IReadOnlyList<StockValuation>>> ValueWarehouseAsync(DateOnly asOfDate, CancellationToken cancellationToken = default);
    Task<WarehouseAnalysis<IReadOnlyList<ReorderAnalysis>>> AnalyzeReorderAsync(DateOnly analysisDate, CancellationToken cancellationToken = default);
    Task<WarehouseAnalysis<IReadOnlyList<SupplierPerformance>>> AnalyzeSuppliersAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<WarehouseAnalysis<RequisitionMatch>> MatchRequisitionAsync(string requisitionId, DateOnly stockDate, CancellationToken cancellationToken = default);
    Task<WarehouseAnalysis<IReadOnlyList<RequisitionMatch>>> MatchPeriodAsync(DateOnly start, DateOnly end, DateOnly stockDate, CancellationToken cancellationToken = default);
    Task<WarehouseAnalysis<WarehouseOverview>> BuildOverviewAsync(DateOnly analysisDate, CancellationToken cancellationToken = default);
    Task<WarehouseAnalysis<IReadOnlyList<DailyMovement>>> BuildDailyMovementAsync(DateOnly date, CancellationToken cancellationToken = default);
}
