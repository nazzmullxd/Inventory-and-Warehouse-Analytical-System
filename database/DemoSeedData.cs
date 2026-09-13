using Iwas.Model.Data.InMemory;
using Iwas.Model.Source;
using Iwas.Presentation.Infrastructure;

namespace Iwas.Database;

/// <summary>Deterministic demonstration data; never used as a runtime fallback.</summary>
public static class DemoSeedData
{
    public const string Version = "IWAS-DEMO-2026-07-08-V2";

    public static WarehouseSnapshot Create()
    {
        var original = DemoWarehouse.Create();
        var extra = Additions();
        return original with
        {
            Items = original.Items.Concat(extra.Items).ToArray(),
            StockMovements = original.StockMovements.Concat(extra.StockMovements).ToArray(),
            SupplierDeliveries = original.SupplierDeliveries.Concat(extra.SupplierDeliveries).ToArray(),
            Requisitions = original.Requisitions.Concat(extra.Requisitions).ToArray(),
            SourceVersion = Version
        };
    }

    public static WarehouseSnapshot Additions()
    {
        var day = new DateOnly(2026, 7, 8);
        var items = new[]
        {
            Item("IT-6001", "Spiral Notebook", "Stationery", "ruled spiral notebook hardcover", 85m),
            Item("IT-6002", "Document Folder", "Stationery", "blue document folder elastic closure", 45m),
            Item("IT-6003", "Whiteboard Marker", "Stationery", "black whiteboard marker dry erase", 60m),
            Item("IT-6004", "Packing Tape", "Packaging", "clear packing tape adhesive roll", 95m),
            Item("IT-6005", "Shipping Carton", "Packaging", "corrugated shipping carton medium", 120m),
            Item("IT-6006", "Bubble Wrap Roll", "Packaging", "protective bubble wrap roll", 650m),
            Item("IT-6007", "Safety Gloves", "Safety", "industrial safety gloves nitrile coated", 180m),
            Item("IT-6008", "Safety Helmet", "Safety", "yellow safety helmet adjustable", 450m),
            Item("IT-6009", "Reflective Vest", "Safety", "orange reflective safety vest", 320m),
            Item("IT-6010", "Hand Sanitizer", "Cleaning", "alcohol hand sanitizer pump bottle", 220m),
            Item("IT-6011", "Floor Cleaner", "Cleaning", "lemon floor cleaner disinfectant bottle", 175.50m),
            Item("IT-6012", "Cotton Mop", "Cleaning", "cotton floor mop wooden handle", 280m),
            Item("IT-6013", "Wireless Mouse", "IT", "wireless optical mouse silent click", 750m),
            Item("IT-6014", "USB Keyboard", "IT", "wired usb keyboard full size", 950m),
            Item("IT-6015", "Network Cable", "IT", "ethernet network cable patch lead", 250m),
            Item("IT-6016", "Desk Organizer", "Office", "mesh desktop organizer compartments", 390m),
            Item("IT-6017", "Desk Organizer, Large", "Office", "mesh desktop organizer compartments", 490m),
            Item("IT-6018", "Thermal Label Roll", "Packaging", "thermal shipping label adhesive roll", null),
            Item("IT-6019", "Legacy Printer Ribbon", "IT", "legacy dot matrix printer ribbon", 300m),
            Item("IT-6020", "Archive Storage Box", "Office", "archive document storage box lid", 210m)
        };
        var movements = new List<StockMovementRecord>();
        var deliveries = new List<SupplierDeliveryRecord>();
        var requisitions = new List<RequisitionRecord>();
        var suppliers = new[]
        {
            ("SUP-10", "Bengal Office Supply"), ("SUP-11", "Dhaka Packaging House"),
            ("SUP-12", "SafeWork Bangladesh"), ("SUP-13", "CleanSpace Distributors"),
            ("SUP-14", "Delta Technology"), ("SUP-15", "Eastern General Traders")
        };
        var departments = new[] { "Accounts", "Admin", "Warehouse", "Dispatch", "IT" };
        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            var price = item.CatalogueUnitPrice ?? 420m;
            var monthlyDemand = 12m + index * 2;
            var deadStock = index >= 18;
            // Three low/zero-stock items; older opening layers remain fully priced.
            var closing = index == 12 ? 0m : index % 6 == 0 ? 2m : 100m + index * 5;
            var opening = deadStock ? 80m : 12 * monthlyDemand + closing;
            movements.Add(Move("OPEN", new(2025, 7, 1), StockMovementType.Receipt, opening, price * .9m));
            if (deadStock)
            {
                movements.Add(Move("OLD-ISSUE", new(2025, 12, 1), StockMovementType.Issue, 10));
            }
            else
            {
                for (var month = 0; month < 12; month++)
                    movements.Add(Move($"ISSUE-{month + 1:00}", new DateOnly(2025, 7, 15).AddMonths(month), StockMovementType.Issue, monthlyDemand));
                // Equal same-day receipt/issue amounts exercise sequencing and daily movement reports.
                movements.Add(Move("JUL-RECEIPT", day, StockMovementType.Receipt, 20, price, 1));
                movements.Add(Move("JUL-ISSUE", day, StockMovementType.Issue, 20, sequence: 2));
            }

            var supplier = suppliers[index / 4 % suppliers.Length];
            if (index >= 18) supplier = suppliers[5];
            for (var month = 1; month <= 6; month++)
            {
                var promised = new DateOnly(2026, month, 5 + index % 10);
                var delay = index % 3 == 0 ? 0 : index % 3 == 1 ? (month % 3 == 0 ? 3 : 0) : 2 + month % 4;
                var key = $"V2-{index + 1:00}-{month:00}";
                deliveries.Add(new($"DL-{key}", $"PO-{key}", supplier.Item1, supplier.Item2, item.ItemId, promised, promised.AddDays(delay)));
            }
            var openKey = $"V2-{index + 1:00}-OPEN";
            deliveries.Add(new($"DL-{openKey}", $"PO-{openKey}", supplier.Item1, supplier.Item2, item.ItemId, new(2026, 6, 28), null));

            requisitions.Add(new($"RQ-{900 + index:0000}", departments[index % departments.Length], day,
                $"{(index % 6 == 0 ? 25 : 3)} each {item.Description}"));
            requisitions.Add(new($"RQ-{920 + index:0000}", departments[(index + 1) % departments.Length], day.AddDays(-1 - index % 7),
                $"2 each {item.Description}"));

            StockMovementRecord Move(string suffix, DateOnly date, StockMovementType type, decimal quantity, decimal? cost = null, int sequence = 1) =>
                new($"V2-{index + 1:00}-{suffix}", item.ItemId, date, sequence, type, quantity, cost);
        }
        requisitions.AddRange(new[]
        {
            new RequisitionRecord("RQ-0940", "Admin", day, "replacement accessories for the meeting room"),
            new RequisitionRecord("RQ-0941", "Warehouse", day, "গুদামের জন্য বিশেষ সরঞ্জাম প্রয়োজন"),
            new RequisitionRecord("RQ-0942", "Dispatch", day, ""),
            new RequisitionRecord("RQ-0943", "IT", day, "wired usb keyboard full size"),
            new RequisitionRecord("RQ-0944", "Dispatch", day, "2 boxes clear packing tape adhesive roll"),
            new RequisitionRecord("RQ-0945", "Admin", day, "3 each mesh desktop organizer compartments")
        });
        return new(items, movements, deliveries, requisitions, Version, new DateOnly(2025, 7, 1), true, "expanded demonstration snapshot");
    }

    private static ItemRecord Item(string id, string name, string category, string description, decimal? price) =>
        new(id, name, "each", category, Math.Round((price ?? 420m) * .15m, 4), 250m,
            description, price, new("demo-catalogue-v2", price is null ? "Awaiting supplier quotation" : null));
}
