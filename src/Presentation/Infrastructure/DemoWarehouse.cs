using Iwas.Model.Data.InMemory;
using Iwas.Model.Source;

namespace Iwas.Presentation.Infrastructure;

public static class DemoWarehouse
{
    public static WarehouseSnapshot Create()
    {
        var analysis = new DateOnly(2026, 7, 8);
        var items = new[]
        {
            new ItemRecord("IT-1108", "A4 Paper", "ream", "Stationery", 40, 800, "white A4 office paper ream", 550, new("demo-catalogue")),
            new ItemRecord("IT-3320", "Gel Pen, Black", "each", "Stationery", 3, 20, "black gel pen smooth ink", 15, new("demo-catalogue")),
            new ItemRecord("IT-2201", "Toner Cartridge", "each", "IT", 600, 1500, "laser printer toner cartridge black", 5350, new("demo-catalogue")),
            new ItemRecord("IT-4401", "Stapler", "each", "Stationery", 8, 40, "medium office metal stapler", 250, new("demo-catalogue")),
            new ItemRecord("IT-5501", "Fax Machine Ribbon", "each", "IT", 20, 100, "fax machine replacement ribbon", 200, new("demo-catalogue"))
        };
        var movements = new[]
        {
            M("P-OPEN", "IT-1108", new(2025,7,1), 1, StockMovementType.Receipt, 3500, 500),
            M("P-R1", "IT-1108", new(2026,7,2), 1, StockMovementType.Receipt, 100, 520),
            M("P-R2", "IT-1108", new(2026,7,4), 1, StockMovementType.Receipt, 80, 535),
            M("P-R3", "IT-1108", new(2026,7,6), 1, StockMovementType.Receipt, 60, 550),
            M("P-I1", "IT-1108", new(2026,7,7), 1, StockMovementType.Issue, 3650),
            M("G-R1", "IT-3320", new(2025,7,1), 1, StockMovementType.Receipt, 795, 15),
            M("G-I1", "IT-3320", new(2026,6,20), 1, StockMovementType.Issue, 365),
            M("T-R1", "IT-2201", new(2025,7,1), 1, StockMovementType.Receipt, 431, 5200),
            M("T-I1", "IT-2201", new(2026,7,4), 1, StockMovementType.Issue, 400),
            M("S-R1", "IT-4401", new(2025,7,1), 1, StockMovementType.Receipt, 75, 250),
            M("S-I1", "IT-4401", new(2026,6,1), 1, StockMovementType.Issue, 20),
            M("F-R1", "IT-5501", new(2025,7,1), 1, StockMovementType.Receipt, 54, 200),
            M("F-I1", "IT-5501", new(2025,12,1), 1, StockMovementType.Issue, 10)
        };
        var deliveries = Enumerable.Range(1, 20).Select(i =>
        {
            var promised = new DateOnly(2026, 1, 10).AddDays(i * 5);
            var delay = i <= 17 ? 0 : (i - 17) * 2;
            return new SupplierDeliveryRecord($"DL-{i:00}", $"PO-{i:00}", "SUP-07", "Meghna Traders", i % 2 == 0 ? "IT-1108" : "IT-3320", promised, promised.AddDays(delay));
        }).Concat(new[]
        {
            D("J1", "SUP-08", "Jamuna Supplies", "IT-2201", new(2026,3,1), 8),
            D("J2", "SUP-08", "Jamuna Supplies", "IT-2201", new(2026,4,1), 0),
            D("J3", "SUP-08", "Jamuna Supplies", "IT-2201", new(2026,5,1), 5),
            D("S1", "SUP-09", "Surma Stationers", "IT-4401", new(2026,2,1), 0),
            D("S2", "SUP-09", "Surma Stationers", "IT-5501", new(2026,5,1), 0)
        }).ToArray();
        var requisitions = new[]
        {
            new RequisitionRecord("RQ-0871", "Accounts", analysis, "Black gel pen smooth ink, one dozen each"),
            new RequisitionRecord("RQ-0872", "Admin", analysis, "two reams white A4 office paper"),
            new RequisitionRecord("RQ-0873", "IT", analysis, "replacement connection accessories for meeting room equipment"),
            new RequisitionRecord("RQ-0874", "Admin", analysis, "one box black writing instruments"),
            new RequisitionRecord("RQ-0875", "Store", analysis, "বিশেষ যন্ত্রের জন্য একটি প্রতিস্থাপন অংশ প্রয়োজন।"),
            new RequisitionRecord("RQ-0876", "IT", analysis, "")
        };
        return new(items, movements, deliveries, requisitions, "IWAS-DEMO-2026-07-08", new DateOnly(2025, 7, 1), true, "coherent demonstration snapshot");
    }

    private static StockMovementRecord M(string id, string item, DateOnly date, int sequence, StockMovementType type, decimal quantity, decimal? price = null) => new(id, item, date, sequence, type, quantity, price);
    private static SupplierDeliveryRecord D(string id, string supplier, string name, string item, DateOnly promised, int delay) => new(id, id, supplier, name, item, promised, promised.AddDays(delay));
}
