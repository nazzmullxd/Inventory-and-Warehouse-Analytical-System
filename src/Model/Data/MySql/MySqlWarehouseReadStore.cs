using System.Data;
using Iwas.Model.Contracts.Common;
using Iwas.Model.Contracts.Queries;
using Iwas.Model.Data.InMemory;
using Iwas.Model.Repositories;
using Iwas.Model.Source;
using MySqlConnector;

namespace Iwas.Model.Data.MySql;

/// <summary>Loads a bounded, consistent database extract per analytical operation.
/// Queries share the existing canonical validation and filtering contract.</summary>
public sealed class MySqlWarehouseReadStore(string connectionString, QueryLimits? limits = null) : IWarehouseReadStore
{
    private readonly QueryLimits _limits = limits ?? new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private (InMemoryWarehouseReadStore Store, ReadContextMetadata Metadata)? _extract;

    public async ValueTask<IWarehouseReadContext> OpenReadContextAsync(CancellationToken cancellationToken = default)
    {
        // Register this store as scoped: dashboard/report subcalls must share one request extract.
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            _extract ??= await LoadAsync(cancellationToken);
            return _extract.Value.Store.CreateContext(_extract.Value.Metadata);
        }
        finally { _loadLock.Release(); }
    }

    private async Task<(InMemoryWarehouseReadStore Store, ReadContextMetadata Metadata)> LoadAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, true, cancellationToken);

        // All mapped tables must retain InnoDB snapshot semantics.
        var engines = await Read("SELECT TABLE_NAME, ENGINE FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME IN ('warehouse_metadata','items','stock_movements','supplier_deliveries','requisitions')", 5,
            r => r.GetString(1));
        if (engines.Count != 5 || engines.Any(x => x != "InnoDB"))
            throw new InvalidOperationException("IWAS requires all five schema tables to use InnoDB.");

        var metadata = await Read("SELECT schema_version, source_version, reliable_history_start, has_trusted_opening_stock, source_name FROM warehouse_metadata WHERE id = 1", 1,
            r => new { Schema = r.GetInt32(0), Version = r.GetString(1), Start = Date(r, 2), Trusted = r.GetBoolean(3), Name = r.GetString(4) });
        if (metadata.Count != 1 || metadata[0].Schema != 1)
            throw new InvalidOperationException("IWAS database schema is missing or incompatible. Run database setup.");
        var m = metadata[0];
        var items = await Read("SELECT item_id, name, unit, category, holding_cost, ordering_cost, description, catalogue_unit_price, price_source, price_availability_reason FROM items ORDER BY item_id", _limits.MaxItems,
            r => new ItemRecord(r.GetString(0), r.GetString(1), r.GetString(2), Text(r, 3), r.GetDecimal(4), r.GetDecimal(5), r.GetString(6), Number(r, 7), r.IsDBNull(8) ? null : new PriceProvenance(r.GetString(8), Text(r, 9))));
        var movements = await Read("SELECT movement_id, item_id, movement_date, movement_sequence, movement_type, quantity, unit_purchase_price FROM stock_movements ORDER BY item_id, movement_date, movement_sequence", _limits.MaxMovements,
            r => new StockMovementRecord(r.GetString(0), r.GetString(1), Date(r, 2)!.Value, r.IsDBNull(3) ? null : r.GetInt32(3), r.GetString(4) switch { "Receipt" => StockMovementType.Receipt, "Issue" => StockMovementType.Issue, _ => throw new InvalidOperationException("Invalid stock movement type.") }, r.GetDecimal(5), Number(r, 6)));
        var deliveries = await Read("SELECT delivery_record_key, purchase_order_id, supplier_id, supplier_name, item_id, promised_delivery_date, actual_delivery_date FROM supplier_deliveries ORDER BY delivery_record_key", _limits.MaxDeliveries,
            r => new SupplierDeliveryRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), Date(r, 5)!.Value, Date(r, 6)));
        var requisitions = await Read("SELECT requisition_id, department, requisition_date, description FROM requisitions ORDER BY requisition_id", _limits.MaxRequisitions,
            r => new RequisitionRecord(r.GetString(0), r.GetString(1), Date(r, 2)!.Value, r.GetString(3)));
        await transaction.CommitAsync(cancellationToken);
        var snapshot = new WarehouseSnapshot(items, movements, deliveries, requisitions, m.Version, m.Start, m.Trusted, m.Name);
        // This identifier describes this request, not a replayable historical database version.
        var contextMetadata = new ReadContextMetadata(m.Version, "mysql-v1", DateTimeOffset.UtcNow,
            $"request:{Guid.NewGuid():N}", ConsistencyMode.TransactionSnapshot, m.Start, m.Trusted, m.Name);
        return (new InMemoryWarehouseReadStore(snapshot, _limits), contextMetadata);

        async Task<List<T>> Read<T>(string sql, int maximum, Func<MySqlDataReader, T> map)
        {
            await using var command = new MySqlCommand(sql + " LIMIT @limit", connection, transaction);
            command.Parameters.AddWithValue("@limit", checked(maximum + 1));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<T>();
            while (await reader.ReadAsync(cancellationToken))
            {
                if (rows.Count == maximum)
                    throw new InvalidOperationException("Warehouse source exceeds the configured record limit; analysis was not truncated.");
                rows.Add(map(reader));
            }
            return rows;
        }
    }

    private static string? Text(MySqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static decimal? Number(MySqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetDecimal(index);
    private static DateOnly? Date(MySqlDataReader reader, int index) => reader.IsDBNull(index) ? null : DateOnly.FromDateTime(reader.GetDateTime(index));
}
