using Iwas.Business.Stock;
using Iwas.Model.Contracts.Common;
using Iwas.Model.Source;

namespace Iwas.Business.Valuation;

public sealed record ValuedStockLayer(StockLayer Layer, decimal LayerValue);
public sealed record StockValuation(
    string ItemId, string Name, string Unit, DateOnly AsOfDate,
    decimal ClosingQuantity, decimal FifoValue, IReadOnlyList<ValuedStockLayer> Layers);

public sealed class FifoValuationService(StockLedgerService ledger)
{
    public AnalysisResult<StockValuation> Value(
        ItemRecord item, DateOnly asOfDate, IEnumerable<StockMovementRecord> movements,
        bool historyCoverageComplete = true)
    {
        var input = movements.Where(x => string.Equals(x.ItemId.Trim(), item.ItemId.Trim(), StringComparison.OrdinalIgnoreCase)
                                         && x.MovementDate <= asOfDate).ToArray();
        var invalidPrice = input.FirstOrDefault(x => x.MovementType == StockMovementType.Receipt && x.UnitPurchasePrice is null or < 0);
        if (invalidPrice is not null)
            return AnalysisResult<StockValuation>.Failure(new AnalysisIssue(
                invalidPrice.UnitPurchasePrice is null ? "receipt_price_missing" : "validation_failed",
                IssueSeverity.Blocking, "Every receipt requires a non-negative purchase price for FIFO valuation.",
                invalidPrice.MovementId, "UnitPurchasePrice"));

        var position = ledger.Calculate(item.ItemId, asOfDate, input, historyCoverageComplete);
        if (!position.IsComplete)
            return new(null, position.Issues);

        var valued = position.Data!.RemainingLayers
            .Select(x => new ValuedStockLayer(x, x.RemainingQuantity * x.UnitPurchasePrice!.Value)).ToArray();
        var warnings = input.Any(x => x.MovementType == StockMovementType.Receipt && x.UnitPurchasePrice == 0)
            ? new[] { new AnalysisIssue("zero_receipt_price", IssueSeverity.Warning, "A receipt has a zero purchase price.", item.ItemId) }
            : Array.Empty<AnalysisIssue>();
        return AnalysisResult<StockValuation>.Success(new(
            item.ItemId, item.Name, item.Unit, asOfDate, position.Data.ClosingQuantity,
            valued.Sum(x => x.LayerValue), valued), warnings);
    }
}
