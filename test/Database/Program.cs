using System.Text.Json;
using Iwas.Model.Contracts.Common;
using Iwas.Model.Contracts.Queries;
using Iwas.Model.Data.MySql;
using Iwas.Business.Stock;
using Iwas.Business.Valuation;
using Iwas.Presentation.Infrastructure;
using MySqlConnector;
using Iwas.Database;

// Read-only integration checks against the seeded local database. Never issue trial writes here.
using var config = JsonDocument.Parse(await File.ReadAllTextAsync("src/Presentation/appsettings.Local.json"));
var connectionString = config.RootElement.GetProperty("ConnectionStrings").GetProperty("Warehouse").GetString()!;
var store = new MySqlWarehouseReadStore(connectionString);
await using var context = await store.OpenReadContextAsync();
var seed = context.Metadata.SourceVersion == DemoSeedData.Version ? DemoSeedData.Create() : DemoWarehouse.Create();
var day = new DateOnly(2026, 7, 8);
var items = await context.SearchItemsAsync(new(), new(1, 500));
Assert(items.IsComplete && items.Data!.Rows.OrderBy(x => x.ItemId).SequenceEqual(seed.Items.OrderBy(x => x.ItemId)), "item mappings and decimal prices");
var movements = await context.GetStockHistoryAsync(new(seed.Items.Select(x => x.ItemId).ToArray(), day));
Assert(movements.IsComplete && movements.Data!.OrderBy(x => x.MovementId).SequenceEqual(seed.StockMovements.OrderBy(x => x.MovementId)), "movement mapping, null issue prices and chronology");
var deliveries = await context.GetDeliveredOrdersAsync(new(new(new(2026, 1, 1), day)));
Assert(deliveries.IsComplete && deliveries.Data!.OrderBy(x => x.DeliveryRecordKey).SequenceEqual(seed.SupplierDeliveries.Where(x => x.ActualDeliveryDate != null).OrderBy(x => x.DeliveryRecordKey)), "supplier delivery mappings");
var openDeliveries = await context.GetOpenDeliveriesAsync(new(new(new(2026, 1, 1), day)));
Assert(openDeliveries.IsComplete && openDeliveries.Data!.OrderBy(x => x.DeliveryRecordKey).SequenceEqual(seed.SupplierDeliveries.Where(x => x.ActualDeliveryDate == null).OrderBy(x => x.DeliveryRecordKey)), "open delivery dates remain null");
var requisitions = await context.GetRequisitionsAsync(new(new(day, day)), new());
Assert(requisitions.IsComplete && requisitions.Data!.Rows.SequenceEqual(seed.Requisitions.Where(x => x.RequisitionDate == day).OrderBy(x => x.RequisitionId)), "Unicode requisitions and inclusive date boundaries");
var allRequisitions = await context.GetRequisitionsAsync(new(new(day.AddDays(-7), day)), new(1, 100));
Assert(allRequisitions.IsComplete && allRequisitions.Data!.Rows.SequenceEqual(seed.Requisitions.OrderBy(x => x.RequisitionDate).ThenBy(x => x.RequisitionId)), "historical requisitions and full-period counts");
var paper = await context.GetItemAsync(" it-1108 ");
var valuation = new FifoValuationService(new StockLedgerService()).Value(paper.Data!, day, movements.Data!);
Assert(valuation.IsComplete && valuation.Data!.ClosingQuantity == 90m && valuation.Data.FifoValue == 49050m, "database-backed FIFO: 90 units / 49050 BDT");
foreach (var item in items.Data!.Rows)
{
    var result = new FifoValuationService(new StockLedgerService()).Value(item, day, movements.Data!);
    Assert(result.IsComplete && result.Data!.ClosingQuantity >= 0, $"valid FIFO history: {item.ItemId}");
}
if (context.Metadata.SourceVersion == DemoSeedData.Version)
{
    var matcher = new Iwas.Business.Matching.RequisitionMatchingService(new StockLedgerService());
    var matches = requisitions.Data!.Rows.ToDictionary(x => x.RequisitionId,
        x => matcher.Match(x, items.Data.Rows, day, movements.Data!).Data!);
    Assert(matches["RQ-0912"].StockOnHand == 0 && matches["RQ-0912"].HasSufficientStock == false, "zero-stock requisition");
    Assert(matches["RQ-0918"].HasSufficientStock == true, "available legacy stock");
    Assert(matches["RQ-0917"].UnitPrice == null, "missing catalogue price");
    Assert(matches["RQ-0945"].Reason == Iwas.Business.Matching.MatchReason.AmbiguousTopScore, "tied catalogue candidates");
    Assert(matches["RQ-0943"].Quantity.Status == Iwas.Business.Matching.QuantityStatus.Missing, "missing requisition quantity");
    Assert(matches["RQ-0944"].UnitCompatibility == Iwas.Business.Matching.UnitCompatibility.Incompatible, "box versus each unit mismatch");
}
Assert(context.Metadata.ConsistencyMode == ConsistencyMode.TransactionSnapshot && context.Metadata.MappingVersion == "mysql-v1", "database snapshot provenance");
await using (var second = await store.OpenReadContextAsync())
    Assert(second.Metadata.SnapshotId == context.Metadata.SnapshotId, "analytical subcalls share the request extract");
await using (var nextRequest = await new MySqlWarehouseReadStore(connectionString).OpenReadContextAsync())
    Assert(nextRequest.Metadata.SnapshotId != context.Metadata.SnapshotId, "separate requests have distinct extracts");
await Throws<OperationCanceledException>(async () => await store.OpenReadContextAsync(new CancellationToken(true)), "cancellation");
await Throws<InvalidOperationException>(async () => await new MySqlWarehouseReadStore(connectionString, new QueryLimits(MaxItems: 1)).OpenReadContextAsync(), "limits reject oversized extracts");
await context.DisposeAsync();
await Throws<ObjectDisposedException>(async () => await context.GetItemAsync("IT-1108"), "disposed context");
await using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();
await using var command = new MySqlCommand("SHOW GRANTS FOR CURRENT_USER", connection);
await using var reader = await command.ExecuteReaderAsync();
var grants = new List<string>();
while (await reader.ReadAsync()) grants.Add(reader.GetString(0));
Assert(grants.Count == 2 && grants.Any(x => x.StartsWith("GRANT USAGE ON *.*")) && grants.Any(x => x.StartsWith("GRANT SELECT ON `iwas`.*")) && grants.All(x => !x.Contains("WITH GRANT OPTION")), "runtime identity has only SELECT access");
Console.WriteLine("All database integration checks passed.");

static void Assert(bool condition, string name)
{
    if (!condition) throw new Exception($"FAIL {name}");
    Console.WriteLine($"PASS {name}");
}
static async Task Throws<T>(Func<Task> action, string name) where T : Exception
{
    try { await action(); }
    catch (T) { Console.WriteLine($"PASS {name}"); return; }
    throw new Exception($"FAIL {name}: expected {typeof(T).Name}");
}
