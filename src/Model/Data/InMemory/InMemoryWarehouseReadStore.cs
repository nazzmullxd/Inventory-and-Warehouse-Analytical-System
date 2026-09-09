using Iwas.Model.Contracts.Common;
using Iwas.Model.Contracts.Queries;
using Iwas.Model.Data.Validation;
using Iwas.Model.Repositories;
using Iwas.Model.Source;

namespace Iwas.Model.Data.InMemory;

public sealed record WarehouseSnapshot(
    IReadOnlyCollection<ItemRecord> Items,
    IReadOnlyCollection<StockMovementRecord> StockMovements,
    IReadOnlyCollection<SupplierDeliveryRecord> SupplierDeliveries,
    IReadOnlyCollection<RequisitionRecord> Requisitions,
    string SourceVersion,
    DateOnly? ReliableHistoryStart = null,
    bool HasTrustedOpeningStock = false,
    string SourceName = "in-memory");

/// <summary>A deterministic read-only adapter for tests, demonstrations, and future adapter contract tests.</summary>
public sealed class InMemoryWarehouseReadStore : IWarehouseReadStore
{
    private readonly WarehouseSnapshot _snapshot;
    private readonly QueryLimits _limits;

    public InMemoryWarehouseReadStore(WarehouseSnapshot snapshot, QueryLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshot = snapshot with
        {
            Items = snapshot.Items.ToArray(),
            StockMovements = snapshot.StockMovements.ToArray(),
            SupplierDeliveries = snapshot.SupplierDeliveries.ToArray(),
            Requisitions = snapshot.Requisitions.ToArray()
        };
        _limits = limits ?? new();
    }

    public ValueTask<IWarehouseReadContext> OpenReadContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadata = new ReadContextMetadata(_snapshot.SourceVersion, "canonical-v1", DateTimeOffset.UtcNow,
            _snapshot.SourceVersion, ConsistencyMode.ImmutableExtract, _snapshot.ReliableHistoryStart,
            _snapshot.HasTrustedOpeningStock, _snapshot.SourceName);
        return ValueTask.FromResult<IWarehouseReadContext>(new Context(_snapshot, _limits, metadata));
    }

    private sealed class Context : IWarehouseReadContext
    {
        private readonly ItemRecord[] _items;
        private readonly StockMovementRecord[] _movements;
        private readonly SupplierDeliveryRecord[] _deliveries;
        private readonly RequisitionRecord[] _requisitions;
        private readonly QueryLimits _limits;
        private readonly IReadOnlyList<AnalysisIssue> _validationIssues;
        private bool _disposed;

        public Context(WarehouseSnapshot snapshot, QueryLimits limits, ReadContextMetadata metadata)
        {
            _items = snapshot.Items.ToArray();
            _movements = snapshot.StockMovements.ToArray();
            _deliveries = snapshot.SupplierDeliveries.ToArray();
            _requisitions = snapshot.Requisitions.ToArray();
            _limits = limits;
            Metadata = metadata;
            _validationIssues = SourceDataValidator.Validate(_items, _movements, _deliveries, _requisitions, limits);
        }

        public ReadContextMetadata Metadata { get; }

        public ValueTask<QueryResult<ItemRecord>> GetItemAsync(string itemId, CancellationToken cancellationToken = default)
        {
            Guard(cancellationToken);
            if (string.IsNullOrWhiteSpace(itemId)) return ValueTask.FromResult(Invalid<ItemRecord>("An item ID is required.", "ItemId"));
            if (HasBlockingValidation) return ValueTask.FromResult(Failed<ItemRecord>());
            var item = _items.SingleOrDefault(x => SameId(x.ItemId, itemId));
            return ValueTask.FromResult(item is null
                ? QueryResult<ItemRecord>.Failure(Metadata, new AnalysisIssue(IssueCodes.EntityNotFound, IssueSeverity.Blocking, "Item was not found.", itemId))
                : QueryResult<ItemRecord>.Success(item, Metadata));
        }

        public ValueTask<QueryResult<PageResult<ItemRecord>>> SearchItemsAsync(ItemSearch query, PageRequest page, CancellationToken cancellationToken = default)
        {
            Guard(cancellationToken);
            if (!ValidPage(page, out var issue)) return ValueTask.FromResult(QueryResult<PageResult<ItemRecord>>.Failure(Metadata, issue!));
            if (HasBlockingValidation) return ValueTask.FromResult(Failed<PageResult<ItemRecord>>());
            var filtered = _items.Where(x => string.IsNullOrWhiteSpace(query.Text) || x.ItemId.Contains(query.Text, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(query.Text, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(query.Category) || string.Equals(x.Category, query.Category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase).ToArray();
            return ValueTask.FromResult(QueryResult<PageResult<ItemRecord>>.Success(Page(filtered, page), Metadata));
        }

        public ValueTask<QueryResult<IReadOnlyList<ItemRecord>>> GetItemsAsync(IReadOnlyCollection<string> itemIds, CancellationToken cancellationToken = default)
        {
            Guard(cancellationToken);
            if (itemIds.Count > _limits.MaxItems) return ValueTask.FromResult(TooLarge<IReadOnlyList<ItemRecord>>("ItemIds"));
            if (HasBlockingValidation) return ValueTask.FromResult(Failed<IReadOnlyList<ItemRecord>>());
            var wanted = itemIds.Select(SourceDataValidator.Canonical).ToHashSet(StringComparer.Ordinal);
            IReadOnlyList<ItemRecord> rows = _items.Where(x => wanted.Contains(SourceDataValidator.Canonical(x.ItemId))).OrderBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase).ToArray();
            return ValueTask.FromResult(QueryResult<IReadOnlyList<ItemRecord>>.Success(rows, Metadata));
        }

        public ValueTask<QueryResult<IReadOnlyList<StockMovementRecord>>> GetStockHistoryAsync(StockHistoryQuery query, CancellationToken cancellationToken = default)
        {
            Guard(cancellationToken);
            if (query.ItemIds.Count > _limits.MaxItems) return ValueTask.FromResult(TooLarge<IReadOnlyList<StockMovementRecord>>("ItemIds"));
            if (HasBlockingValidation) return ValueTask.FromResult(Failed<IReadOnlyList<StockMovementRecord>>());
            var wanted = query.ItemIds.Select(SourceDataValidator.Canonical).ToHashSet(StringComparer.Ordinal);
            var rows = _movements.Where(x => wanted.Contains(SourceDataValidator.Canonical(x.ItemId)) && x.MovementDate <= query.ThroughDate)
                .OrderBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.MovementDate).ThenBy(x => x.MovementSequence).ToArray();
            return ValueTask.FromResult(Bounded(rows, _limits.MaxMovements, "StockMovements"));
        }

        public ValueTask<QueryResult<IReadOnlyList<StockMovementRecord>>> GetIssueHistoryAsync(IssueHistoryQuery query, CancellationToken cancellationToken = default)
        {
            Guard(cancellationToken);
            if (HasBlockingValidation) return ValueTask.FromResult(Failed<IReadOnlyList<StockMovementRecord>>());
            var wanted = query.ItemIds.Select(SourceDataValidator.Canonical).ToHashSet(StringComparer.Ordinal);
            var rows = _movements.Where(x => x.MovementType == StockMovementType.Issue && wanted.Contains(SourceDataValidator.Canonical(x.ItemId)) && query.Period.Contains(x.MovementDate))
                .OrderBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.MovementDate).ThenBy(x => x.MovementSequence).ToArray();
            return ValueTask.FromResult(Bounded(rows, _limits.MaxMovements, "StockMovements"));
        }

        public ValueTask<QueryResult<IReadOnlyList<SupplierDeliveryRecord>>> GetDeliveredOrdersAsync(DeliveryQuery query, CancellationToken cancellationToken = default)
        {
            Guard(cancellationToken);
            if (HasBlockingValidation) return ValueTask.FromResult(Failed<IReadOnlyList<SupplierDeliveryRecord>>());
            var rows = _deliveries.Where(x => x.ActualDeliveryDate is not null && query.Period.Contains(x.ActualDeliveryDate.Value))
                .Where(x => string.IsNullOrWhiteSpace(query.SupplierId) || SameId(x.SupplierId, query.SupplierId))
                .Where(x => string.IsNullOrWhiteSpace(query.ItemId) || SameId(x.ItemId, query.ItemId))
                .OrderBy(x => x.DeliveryRecordKey, StringComparer.OrdinalIgnoreCase)
                .GroupBy(x => SourceDataValidator.Canonical(x.PurchaseOrderId)).Select(x => x.First())
                .OrderBy(x => x.ActualDeliveryDate).ThenBy(x => x.PurchaseOrderId, StringComparer.OrdinalIgnoreCase).ToArray();
            return ValueTask.FromResult(Bounded(rows, _limits.MaxDeliveries, "SupplierDeliveries"));
        }

        public ValueTask<QueryResult<IReadOnlyList<SupplierDeliveryRecord>>> GetOpenDeliveriesAsync(OpenDeliveryQuery query, CancellationToken cancellationToken = default)
        {
            Guard(cancellationToken);
            if (HasBlockingValidation) return ValueTask.FromResult(Failed<IReadOnlyList<SupplierDeliveryRecord>>());
            var rows = _deliveries.Where(x => x.ActualDeliveryDate is null && query.PromisedPeriod.Contains(x.PromisedDeliveryDate))
                .Where(x => string.IsNullOrWhiteSpace(query.SupplierId) || SameId(x.SupplierId, query.SupplierId))
                .OrderBy(x => x.DeliveryRecordKey, StringComparer.OrdinalIgnoreCase)
                .GroupBy(x => SourceDataValidator.Canonical(x.PurchaseOrderId)).Select(x => x.First())
                .OrderBy(x => x.PromisedDeliveryDate).ThenBy(x => x.PurchaseOrderId, StringComparer.OrdinalIgnoreCase).ToArray();
            return ValueTask.FromResult(Bounded(rows, _limits.MaxDeliveries, "SupplierDeliveries"));
        }

        public ValueTask<QueryResult<RequisitionRecord>> GetRequisitionAsync(string requisitionId, CancellationToken cancellationToken = default)
        {
            Guard(cancellationToken);
            if (string.IsNullOrWhiteSpace(requisitionId)) return ValueTask.FromResult(Invalid<RequisitionRecord>("A requisition ID is required.", "RequisitionId"));
            if (HasBlockingValidation) return ValueTask.FromResult(Failed<RequisitionRecord>());
            var row = _requisitions.SingleOrDefault(x => SameId(x.RequisitionId, requisitionId));
            return ValueTask.FromResult(row is null
                ? QueryResult<RequisitionRecord>.Failure(Metadata, new AnalysisIssue(IssueCodes.EntityNotFound, IssueSeverity.Blocking, "Requisition was not found.", requisitionId))
                : QueryResult<RequisitionRecord>.Success(row, Metadata));
        }

        public ValueTask<QueryResult<PageResult<RequisitionRecord>>> GetRequisitionsAsync(RequisitionQuery query, PageRequest page, CancellationToken cancellationToken = default)
        {
            Guard(cancellationToken);
            if (!ValidPage(page, out var issue)) return ValueTask.FromResult(QueryResult<PageResult<RequisitionRecord>>.Failure(Metadata, issue!));
            if (HasBlockingValidation) return ValueTask.FromResult(Failed<PageResult<RequisitionRecord>>());
            var rows = _requisitions.Where(x => query.Period.Contains(x.RequisitionDate))
                .Where(x => string.IsNullOrWhiteSpace(query.Department) || string.Equals(x.Department, query.Department, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.RequisitionDate).ThenBy(x => x.RequisitionId, StringComparer.OrdinalIgnoreCase).ToArray();
            return ValueTask.FromResult(QueryResult<PageResult<RequisitionRecord>>.Success(Page(rows, page), Metadata));
        }

        public ValueTask DisposeAsync() { _disposed = true; return ValueTask.CompletedTask; }
        private bool HasBlockingValidation => _validationIssues.Any(x => x.Severity == IssueSeverity.Blocking);
        private QueryResult<T> Failed<T>() => QueryResult<T>.Failure(Metadata, _validationIssues.ToArray());
        private QueryResult<T> Invalid<T>(string message, string field) => QueryResult<T>.Failure(Metadata, new AnalysisIssue(IssueCodes.ValidationFailed, IssueSeverity.Blocking, message, Field: field));
        private QueryResult<T> TooLarge<T>(string field) => QueryResult<T>.Failure(Metadata, new AnalysisIssue(IssueCodes.AnalysisInputTooLarge, IssueSeverity.Blocking, "The requested input exceeds the configured limit.", Field: field));
        private QueryResult<IReadOnlyList<T>> Bounded<T>(T[] rows, int maximum, string field) => rows.Length > maximum
            ? TooLarge<IReadOnlyList<T>>(field) : QueryResult<IReadOnlyList<T>>.Success(rows, Metadata);
        private bool ValidPage(PageRequest page, out AnalysisIssue? issue)
        {
            issue = page.PageNumber < 1 || page.PageSize < 1 || page.PageSize > _limits.MaxPageSize
                ? new(IssueCodes.ValidationFailed, IssueSeverity.Blocking, $"Page number must be positive and page size must be between 1 and {_limits.MaxPageSize}.", Field: "Page") : null;
            return issue is null;
        }
        private static PageResult<T> Page<T>(T[] rows, PageRequest page) => new(rows.Skip(page.Offset).Take(page.PageSize).ToArray(), page.PageNumber, page.PageSize, rows.Length);
        private static bool SameId(string left, string right) => string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        private void Guard(CancellationToken token) { token.ThrowIfCancellationRequested(); ObjectDisposedException.ThrowIf(_disposed, this); }
    }
}
