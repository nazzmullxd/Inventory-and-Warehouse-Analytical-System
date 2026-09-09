using Iwas.Model.Contracts.Common;
using Iwas.Model.Contracts.Queries;
using Iwas.Model.Source;

namespace Iwas.Model.Repositories;

public interface IWarehouseReadStore
{
    ValueTask<IWarehouseReadContext> OpenReadContextAsync(CancellationToken cancellationToken = default);
}

public interface IWarehouseReadContext : IAsyncDisposable
{
    ReadContextMetadata Metadata { get; }
    ValueTask<QueryResult<ItemRecord>> GetItemAsync(string itemId, CancellationToken cancellationToken = default);
    ValueTask<QueryResult<PageResult<ItemRecord>>> SearchItemsAsync(ItemSearch query, PageRequest page, CancellationToken cancellationToken = default);
    ValueTask<QueryResult<IReadOnlyList<ItemRecord>>> GetItemsAsync(IReadOnlyCollection<string> itemIds, CancellationToken cancellationToken = default);
    ValueTask<QueryResult<IReadOnlyList<StockMovementRecord>>> GetStockHistoryAsync(StockHistoryQuery query, CancellationToken cancellationToken = default);
    ValueTask<QueryResult<IReadOnlyList<StockMovementRecord>>> GetIssueHistoryAsync(IssueHistoryQuery query, CancellationToken cancellationToken = default);
    ValueTask<QueryResult<IReadOnlyList<SupplierDeliveryRecord>>> GetDeliveredOrdersAsync(DeliveryQuery query, CancellationToken cancellationToken = default);
    ValueTask<QueryResult<IReadOnlyList<SupplierDeliveryRecord>>> GetOpenDeliveriesAsync(OpenDeliveryQuery query, CancellationToken cancellationToken = default);
    ValueTask<QueryResult<RequisitionRecord>> GetRequisitionAsync(string requisitionId, CancellationToken cancellationToken = default);
    ValueTask<QueryResult<PageResult<RequisitionRecord>>> GetRequisitionsAsync(RequisitionQuery query, PageRequest page, CancellationToken cancellationToken = default);
}
