using Iwas.Business.Matching;
using Iwas.Business.Reorder;
using Iwas.Business.Stock;
using Iwas.Business.Suppliers;
using Iwas.Business.Valuation;
using Iwas.Model.Source;

namespace Iwas.Business.Tests;

public sealed class BusinessAcceptanceTests
{
    private static readonly DateOnly AnalysisDate = new(2026, 7, 8);

    [Fact]
    public void Fifo_source_example_returns_90_units_and_49050_value()
    {
        var ledger = new StockLedgerService();
        var service = new FifoValuationService(ledger);
        var item = Item("PAPER", "ream");
        var rows = new[]
        {
            Receipt("R1", 1, 100, 520), Receipt("R2", 2, 80, 535), Receipt("R3", 3, 60, 550),
            Issue("I1", 4, 120), Issue("I2", 5, 30)
        };

        var result = service.Value(item, AnalysisDate, rows);

        Assert.True(result.IsComplete);
        Assert.Equal(90, result.Data!.ClosingQuantity);
        Assert.Equal(49_050, result.Data.FifoValue);
        Assert.Collection(result.Data.Layers,
            x => Assert.Equal((30m, 535m), (x.Layer.RemainingQuantity, x.Layer.UnitPurchasePrice)),
            x => Assert.Equal((60m, 550m), (x.Layer.RemainingQuantity, x.Layer.UnitPurchasePrice)));
    }

    [Fact]
    public void Ledger_rejects_unproven_same_day_order()
    {
        var date = AnalysisDate.AddDays(-1);
        var rows = new[]
        {
            new StockMovementRecord("R", "PAPER", date, null, StockMovementType.Receipt, 10, 1),
            new StockMovementRecord("I", "PAPER", date, null, StockMovementType.Issue, 2)
        };

        var result = new StockLedgerService().Calculate("PAPER", AnalysisDate, rows);

        Assert.False(result.IsComplete);
        Assert.Equal("ambiguous_movement_order", result.Issues.Single().Code);
    }

    [Fact]
    public void Supplier_source_example_returns_85_percent_and_4_day_late_average()
    {
        var rows = Enumerable.Range(1, 20).Select(i => Delivery(i,
            i <= 17 ? 0 : i == 18 ? 2 : i == 19 ? 4 : 6)).ToArray();

        var result = new SupplierPerformanceService().Analyze(new(2026, 1, 1), new(2026, 6, 30), rows);

        var supplier = Assert.Single(result.Data!);
        Assert.Equal(.85m, supplier.OnTimeRatio);
        Assert.Equal(4m, supplier.AverageLateDelayDays);
        Assert.Equal(SupplierStanding.Good, supplier.Standing);
    }

    [Fact]
    public void Reorder_source_example_returns_eoq_382_and_rop_60()
    {
        var item = Item("PAPER", "ream") with { HoldingCostPerUnitPerYear = 40, OrderingCostPerPurchaseOrder = 800 };
        var rows = new[]
        {
            new StockMovementRecord("OPEN", "PAPER", AnalysisDate.AddDays(-400), 1, StockMovementType.Receipt, 3740, 10),
            new StockMovementRecord("DEMAND", "PAPER", AnalysisDate.AddDays(-1), 1, StockMovementType.Issue, 3650)
        };
        var service = new ReorderService(new StockLedgerService(), new SupplierPerformanceService());

        var result = service.Analyze(item, AnalysisDate, rows, []);

        Assert.True(result.IsComplete);
        Assert.InRange(result.Data!.RawEoq, 382.09946m, 382.09947m);
        Assert.Equal(382, result.Data.SuggestedEoq);
        Assert.Equal(10, result.Data.DailyDemand);
        Assert.Equal(60, result.Data.ReorderPoint);
        Assert.False(result.Data.ReorderNow);
        Assert.Equal(3, result.Data.DaysToReorderPoint);
    }

    [Fact]
    public void Matching_exact_80_percent_is_automatic_and_one_dozen_is_12()
    {
        var requisition = new RequisitionRecord("RQ-0871", "Accounts", AnalysisDate, "one dozen blue ballpoint pen fine office");
        var item = Item("IT-3320", "each") with { Description = "blue ballpoint pen medium office", CatalogueUnitPrice = 15 };
        var stock = new[] { new StockMovementRecord("R1", "IT-3320", AnalysisDate, 1, StockMovementType.Receipt, 430, 10) };

        var result = new RequisitionMatchingService(new StockLedgerService()).Match(requisition, [item], AnalysisDate, stock);

        Assert.True(result.IsComplete);
        Assert.Equal(MatchStatus.AutoMatched, result.Data!.Status);
        Assert.Equal(.8, result.Data.Candidates[0].Similarity, 10);
        Assert.Equal(12, result.Data.Quantity.Quantity);
        Assert.Equal(180, result.Data.TotalPrice);
        Assert.Equal(430, result.Data.StockOnHand);
        Assert.True(result.Data.HasSufficientStock == true);
    }

    [Fact]
    public void Matching_exact_top_tie_requires_clarification()
    {
        var request = new RequisitionRecord("RQ", "D", AnalysisDate, "blue pen office");
        var items = new[] { Item("A", "each") with { Description = "blue pen office" }, Item("B", "each") with { Description = "blue pen office" } };

        var result = new RequisitionMatchingService(new StockLedgerService()).Match(request, items, AnalysisDate);

        Assert.Equal(MatchStatus.ClarificationRequired, result.Data!.Status);
        Assert.Equal(MatchReason.AmbiguousTopScore, result.Data.Reason);
    }

    private static ItemRecord Item(string id, string unit) => new(id, id, unit, null, 1, 1, id, null);
    private static StockMovementRecord Receipt(string id, int day, decimal quantity, decimal price) =>
        new(id, "PAPER", AnalysisDate.AddDays(-10 + day), 1, StockMovementType.Receipt, quantity, price);
    private static StockMovementRecord Issue(string id, int day, decimal quantity) =>
        new(id, "PAPER", AnalysisDate.AddDays(-10 + day), 1, StockMovementType.Issue, quantity);
    private static SupplierDeliveryRecord Delivery(int number, int lateDays)
    {
        var promised = new DateOnly(2026, 3, 1).AddDays(number);
        return new($"D{number}", $"PO{number}", "SUP", "Supplier", "PAPER", promised, promised.AddDays(lateDays));
    }
}
