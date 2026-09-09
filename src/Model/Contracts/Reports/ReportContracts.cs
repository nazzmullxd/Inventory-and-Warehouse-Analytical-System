using Iwas.Model.Contracts.Common;
using Iwas.Model.Contracts.Results;

namespace Iwas.Model.Contracts.Reports;

public sealed record ReportMetadata(
    string ReportId,
    string TitleKey,
    DateTimeOffset GeneratedAtUtc,
    string BusinessTimeZone,
    ReadContextMetadata ReadContext,
    IReadOnlyDictionary<string, string> Parameters,
    IReadOnlyList<string> Assumptions,
    bool IsComplete);

public sealed record R1StockMovementRow(
    string ItemId, string ItemName, string Unit, decimal OpeningQuantity,
    decimal ReceivedQuantity, decimal IssuedQuantity, decimal ClosingQuantity,
    int ReceiptMovementCount, int IssueMovementCount);
public sealed record R1Data(ReportMetadata Metadata, DateOnly StockDate, IReadOnlyList<R1StockMovementRow> Rows);

public sealed record R2ValuationLayer(string MovementId, DateOnly ReceiptDate, decimal Quantity, decimal UnitPrice, decimal Value);
public sealed record R2ValuationRow(string ItemId, string ItemName, string Unit, decimal Quantity, decimal Value, IReadOnlyList<R2ValuationLayer> Layers);
public sealed record R2Data(ReportMetadata Metadata, DateOnly StockDate, IReadOnlyList<R2ValuationRow> Rows, AvailableValue<decimal> WarehouseTotal);

public sealed record R3ReorderRow(string ItemId, decimal Stock, decimal ReorderPoint, decimal SuggestedOrderQuantity, string ActionCode);
public sealed record R3DraftLine(string ItemId, decimal Quantity, string Unit, string ReasonCode);
public sealed record R3Data(ReportMetadata Metadata, DateOnly AnalysisDate, IReadOnlyList<R3ReorderRow> Rows, int ActionableCount, IReadOnlyList<R3DraftLine> DraftLines);

public sealed record R4SupplierRow(string SupplierId, string SupplierName, int DeliveredOrders, decimal OnTimeRatio, decimal? AverageLateDelayDays, string StandingCode);
public sealed record R4DeadStockRow(string ItemId, string ItemName, decimal Quantity, AvailableValue<decimal> FifoValue);
public sealed record R4Data(ReportMetadata Metadata, DateOnly SupplierPeriodStart, DateOnly SupplierPeriodEnd, DateOnly StockDate, IReadOnlyList<R4SupplierRow> Suppliers, IReadOnlyList<R4DeadStockRow> DeadStock);

public sealed record R5MatchRow(string RequisitionId, string MatchStatusCode, string ReasonCode, string? MatchedItemId, decimal? Quantity, AvailableValue<decimal> TotalPrice, AvailableValue<decimal> StockOnHand);
public sealed record R5Data(ReportMetadata Metadata, DateOnly PeriodStart, DateOnly PeriodEnd, IReadOnlyList<R5MatchRow> Rows, int MatchedCount, int ClarificationCount);
