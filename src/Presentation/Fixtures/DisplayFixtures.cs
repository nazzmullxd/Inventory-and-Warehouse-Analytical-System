using Iwas.Presentation.ViewModels;

namespace Iwas.Presentation.Fixtures;

// Display-ready examples only. No analytical calculation or persisted source records.
public static class DisplayFixtures
{
    public static readonly Dictionary<string, string> Reports = new()
    {
        ["R1"] = "Daily Stock Movement", ["R2"] = "Stock Valuation (FIFO)",
        ["R3"] = "Reorder List", ["R4"] = "Supplier Performance and Dead Stock",
        ["R5"] = "AI Requisition Matching Log"
    };
    public static readonly DataTable Layers = new("Remaining FIFO layers",
        ["Receipt date", "Remaining quantity", "Unit purchase price", "Layer value"],
        [["06 Jul 2026", "30 reams", "BDT 535.00", "BDT 16,050.00"],
         ["08 Jul 2026", "60 reams", "BDT 550.00", "BDT 33,000.00"]],
        "Closing stock: 90 reams · FIFO value: BDT 49,050.00");
    public static readonly DataTable Reorder = new("Reorder recommendations",
        ["Item / ID", "Unit", "Stock", "Annual demand", "Daily demand", "EOQ", "Effective lead time", "ROP", "Days to ROP", "Dead stock", "Recommendation"],
        [["Toner Cartridge · IT-2201", "piece", "31", "Not supplied", "Not supplied", "45", "Not supplied", "34", "0 days", "No", "Order 45 now"],
         ["A4 Paper · IT-1108", "ream", "90", "3,650", "10", "≈ 382", "5 days", "60", "3 days", "No", "Order in 3 days"],
         ["Stapler · IT-4401", "piece", "55", "Not supplied", "Not supplied", "20", "Not supplied", "6", "Not applicable", "No", "OK"]]);
    public static readonly DataTable Suppliers = new("Supplier delivery reliability",
        ["Supplier / ID", "Delivered orders", "On-time orders", "On-time", "Late orders", "Average late delay", "Standing"],
        [["Meghna Traders · SUP-07", "20", "17", "85%", "3", "4 days", "Good"],
         ["Jamuna Supplies · SUP-08", "14", "Not supplied", "71%", "Not supplied", "7 days", "Watch list"],
         ["Surma Stationers · SUP-09", "9", "Not supplied", "100%", "0", "No late-delivery sample", "Excellent"]]);
    public static readonly DataTable DeadStock = new("Dead stock · no issue in the last 180 days",
        ["Item", "Quantity", "Value", "Valuation basis", "Recommendation"],
        [["Fax Machine Ribbon", "44 units", "BDT 8,800.00", "FIFO (assumed)", "Review for disposal outside IWAS"]]);
    public static readonly DataTable Movement = new("Daily stock movement",
        ["Item", "Unit", "Opening", "Received", "Issued", "Closing"],
        [["A4 Paper", "ream", "60", "60", "30", "90"], ["Gel Pen (Black)", "piece", "442", "0", "12", "430"],
         ["Toner Cartridge", "piece", "25", "10", "4", "31"]], "Movements recorded today: 5 receipts · 9 issues");
    public static readonly DataTable Valuation = new("Warehouse stock valuation",
        ["Item", "Closing quantity", "FIFO layers", "Unit prices (BDT)", "Value (BDT)"],
        [["A4 Paper", "90 reams", "30 + 60", "535.00 / 550.00", "49,050.00"],
         ["Gel Pen (Black)", "430 pieces", "430", "15.00", "6,450.00"],
         ["Toner Cartridge", "31 pieces", "21 + 10", "5,200.00 / 5,350.00", "162,700.00"]],
        "Supplied warehouse total: BDT 218,200.00 · complete illustrative warehouse scope");
    public static readonly DataTable ReorderReport = new("Reorder list",
        ["Item", "Stock", "ROP", "EOQ", "Days to ROP", "Recommendation"],
        [["Toner Cartridge", "31 pieces", "34", "45", "0 days", "Order 45 now"],
         ["A4 Paper", "90 reams", "60", "382", "3 days", "Order in 3 days"],
         ["Stapler", "55 pieces", "6", "20", "Not applicable", "OK"]], "Items to order today: 1");
    public static readonly DataTable MatchingLog = new("Requisition matching log",
        ["Requisition", "Department", "Matched item / best candidate", "Similarity", "Status"],
        [["RQ-0871", "Accounts", "IT-3320 · Gel Pen, Black", "80%", "Priced, in stock"],
         ["RQ-0872", "Admin", "IT-1108 · A4 Paper", "92%", "Priced, in stock"],
         ["RQ-0873", "IT", "Candidate identity not supplied", "51%", "Clarification"]],
        "Requisitions: 3 · Auto-matched: 2 · Clarification: 1");

    // RQ-0871 is the source example. Other detailed texts/states are synthetic UI scenarios.
    public static readonly MatchFixture[] Matches =
    [
        new("RQ-0871", "Accounts", "Black gel pens with smooth ink for the accounts office, one dozen.", "IT-3320 · Gel Pen, Black", "80%", "Matched", "Common terms supplied: black, gel, pen, ink. Source threshold: at least 80%. Similarity is not a probability of correctness.", "12 units", "BDT 15.00 / unit", "BDT 180.00", "430 units", "Sufficient for requested quantity"),
        new("RQ-0872", "Admin", "Please supply two reams of A4 paper for the administration office.", "IT-1108 · A4 Paper", "92%", "Matched", "Higher-score illustrative match. Price and stock supplied as a UI example.", "2 reams", "BDT 550.00 / ream", "BDT 1,100.00", "90 reams", "Sufficient for requested quantity"),
        new("RQ-0873", "IT", "Replacement connection accessories for the meeting room equipment.", "Candidate identity not supplied", "51%", "Clarification", "Similarity below the source threshold. Needs clarification outside IWAS.", "Quantity not identified", "Price unavailable", "Price unavailable", "Not available", "Unknown: quantity not identified"),
        new("RQ-0874", "Admin", "Black writing pens, one box.", "IT-3320 · Gel Pen, Black", "84%", "Clarification", "Tied candidates; supplied status requires clarification outside IWAS.", "Quantity not identified", "Price unavailable", "Price unavailable", "430 units", "Unknown: quantity not identified"),
        new("RQ-0875", "Store", "বিশেষ যন্ত্রের জন্য একটি প্রতিস্থাপন অংশ প্রয়োজন।", "No candidate", "Not available", "Clarification", "No catalogue candidates supplied. Needs clarification outside IWAS.", "Quantity not identified", "Price unavailable", "Price unavailable", "Not available", "Not available"),
        new("RQ-0876", "IT", "", "No candidate", "Not available", "Clarification", "Source text is empty. Needs clarification outside IWAS.", "Quantity not identified", "Price unavailable", "Price unavailable", "Not available", "Not available"),
        new("RQ-0877", "Accounts", "Black gel pens for the accounts office.", "IT-3320 · Gel Pen, Black", "88%", "Matched", "Semantic match retained; a quantity was not identified.", "Quantity not identified", "BDT 15.00 / unit", "Price unavailable", "430 units", "Unknown: quantity not identified"),
        new("RQ-0878", "Accounts", "One dozen black gel pens.", "IT-3320 · Gel Pen, Black", "86%", "Matched", "Semantic match retained; catalogue price is absent.", "12 units", "Price unavailable", "Price unavailable", "430 units", "Sufficient for requested quantity"),
        new("RQ-0879", "Accounts", "Twelve black gel pens for the office.", "IT-3320 · Gel Pen, Black", "90%", "Matched", "Matched item has zero stock in this illustrative scenario.", "12 units", "BDT 15.00 / unit", "BDT 180.00", "0 units", "Insufficient stock"),
        new("RQ-0880", "Accounts", "Five hundred black gel pens for the office.", "IT-3320 · Gel Pen, Black", "90%", "Matched", "Matched item has insufficient stock in this illustrative scenario.", "500 units", "BDT 15.00 / unit", "BDT 7,500.00", "430 units", "Insufficient stock")
    ];
}
