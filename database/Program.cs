using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Iwas.Database;
using Iwas.Presentation.Infrastructure;
using MySqlConnector;

// Run from the repository root. Administrative credentials are setup-only.
var configPath = Path.GetFullPath("src/Presentation/appsettings.Local.json");
if (!File.Exists("database/schema.sql") || !Directory.Exists("src/Presentation"))
    throw new InvalidOperationException("Run this tool from the repository root.");
var adminString = Environment.GetEnvironmentVariable("IWAS_SETUP_CONNECTION")
    ?? "Server=127.0.0.1;Port=3306;User ID=root;Password=;SslMode=None;";
var adminSettings = new MySqlConnectionStringBuilder(adminString) { Database = "" };
await using var connection = new MySqlConnection(adminSettings.ConnectionString);
await connection.OpenAsync();
await Execute("CREATE DATABASE IF NOT EXISTS iwas CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
await connection.ChangeDatabaseAsync("iwas");
using var schemaStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Iwas.DatabaseSetup.schema.sql")!;
using var schemaReader = new StreamReader(schemaStream);
await Execute(await schemaReader.ReadToEndAsync());

await using (var transaction = await connection.BeginTransactionAsync())
{
    await using var count = new MySqlCommand("SELECT (SELECT COUNT(*) FROM warehouse_metadata) + (SELECT COUNT(*) FROM items) + (SELECT COUNT(*) FROM stock_movements) + (SELECT COUNT(*) FROM supplier_deliveries) + (SELECT COUNT(*) FROM requisitions)", connection, transaction);
    var empty = Convert.ToInt64(await count.ExecuteScalarAsync()) == 0;
    var expand = false;
    if (!empty && args.Contains("--add-demo-data"))
    {
        await using var version = new MySqlCommand("SELECT source_version FROM warehouse_metadata WHERE id = 1 FOR UPDATE", connection, transaction);
        var current = (string?)await version.ExecuteScalarAsync();
        if (current == DemoSeedData.Version) Console.WriteLine("Expanded demo seed is already installed.");
        else if (current == DemoWarehouse.Create().SourceVersion) expand = true;
        else throw new InvalidOperationException("Additional demo data can only be applied to the known IWAS demonstration dataset.");
    }
    if (empty || expand)
    {
        var seed = empty ? DemoSeedData.Create() : DemoSeedData.Additions();
        var issues = Iwas.Model.Data.Validation.SourceDataValidator.Validate(
            seed.Items, seed.StockMovements, seed.SupplierDeliveries, seed.Requisitions, new());
        if (issues.Count > 0) throw new InvalidOperationException("Demo seed failed source validation; no records were inserted.");
        foreach (var x in seed.Items)
            await Insert("INSERT INTO items VALUES (@a,@b,@c,@d,@e,@f,@g,@h,@i,@j)", transaction,
                x.ItemId, x.Name, x.Unit, x.Category, x.HoldingCostPerUnitPerYear, x.OrderingCostPerPurchaseOrder, x.Description, x.CatalogueUnitPrice, x.PriceProvenance?.Source, x.PriceProvenance?.AvailabilityReason);
        foreach (var x in seed.StockMovements)
            await Insert("INSERT INTO stock_movements VALUES (@a,@b,@c,@d,@e,@f,@g)", transaction,
                x.MovementId, x.ItemId, x.MovementDate, x.MovementSequence, x.MovementType.ToString(), x.Quantity, x.UnitPurchasePrice);
        foreach (var x in seed.SupplierDeliveries)
            await Insert("INSERT INTO supplier_deliveries VALUES (@a,@b,@c,@d,@e,@f,@g)", transaction,
                x.DeliveryRecordKey, x.PurchaseOrderId, x.SupplierId, x.SupplierName, x.ItemId, x.PromisedDeliveryDate, x.ActualDeliveryDate);
        foreach (var x in seed.Requisitions)
            await Insert("INSERT INTO requisitions VALUES (@a,@b,@c,@d)", transaction,
                x.RequisitionId, x.Department, x.RequisitionDate, x.Description);
        if (empty)
            await Insert("INSERT INTO warehouse_metadata VALUES (1,1,@a,@b,@c,@d)", transaction,
                seed.SourceVersion, seed.ReliableHistoryStart, seed.HasTrustedOpeningStock, "XAMPP MySQL - demonstration data");
        else
            await Insert("UPDATE warehouse_metadata SET source_version = @a WHERE id = 1", transaction, seed.SourceVersion);
        Console.WriteLine($"Seeded {seed.Items.Count} items, {seed.StockMovements.Count} movements, {seed.SupplierDeliveries.Count} deliveries, {seed.Requisitions.Count} requisitions.");
    }
    else Console.WriteLine("Existing data preserved; seeding skipped.");
    await transaction.CommitAsync();
}

string runtimeString;
if (File.Exists(configPath))
{
    using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
    runtimeString = config.RootElement.GetProperty("ConnectionStrings").GetProperty("Warehouse").GetString()!;
    Console.WriteLine("Existing local connection configuration preserved.");
}
else
{
    var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    // CREATE USER deliberately fails if an unmanaged account already exists; never reset its password/grants.
    await Execute($"CREATE USER 'iwas_reader'@'127.0.0.1' IDENTIFIED BY '{password}'");
    await Execute("GRANT SELECT ON iwas.* TO 'iwas_reader'@'127.0.0.1'");
    var runtime = new MySqlConnectionStringBuilder
    {
        Server = "127.0.0.1", Port = adminSettings.Port, Database = "iwas", UserID = "iwas_reader",
        Password = password, SslMode = MySqlSslMode.None, ConnectionTimeout = 5, DefaultCommandTimeout = 30
    };
    runtimeString = runtime.ConnectionString;
    await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(new { ConnectionStrings = new { Warehouse = runtimeString } }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("Created read-only account and ignored appsettings.Local.json (password not printed).");
}
await using var context = await new Iwas.Model.Data.MySql.MySqlWarehouseReadStore(runtimeString).OpenReadContextAsync();
var result = await context.SearchItemsAsync(new(), new());
if (!result.IsComplete) throw new InvalidOperationException("Database source validation failed.");
Console.WriteLine($"Verified runtime connection: {result.Data!.TotalCount} items; {context.Metadata.ConsistencyMode}.");

async Task Execute(string sql)
{
    await using var command = new MySqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
}
async Task Insert(string sql, MySqlTransaction transaction, params object?[] values)
{
    await using var command = new MySqlCommand(sql, connection, transaction);
    for (var i = 0; i < values.Length; i++)
        command.Parameters.AddWithValue($"@{(char)('a' + i)}", values[i] is DateOnly date ? date.ToDateTime(TimeOnly.MinValue) : values[i] ?? DBNull.Value);
    await command.ExecuteNonQueryAsync();
}
