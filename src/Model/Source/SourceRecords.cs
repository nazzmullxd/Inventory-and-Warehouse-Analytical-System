namespace Iwas.Model.Source;

public enum StockMovementType { Receipt, Issue }

public sealed record ItemRecord(
    string ItemId,
    string Name,
    string Unit,
    string? Category,
    decimal HoldingCostPerUnitPerYear,
    decimal OrderingCostPerPurchaseOrder,
    string Description,
    decimal? CatalogueUnitPrice = null,
    PriceProvenance? PriceProvenance = null);

public sealed record PriceProvenance(string Source, string? AvailabilityReason = null);

public sealed record StockMovementRecord(
    string MovementId,
    string ItemId,
    DateOnly MovementDate,
    int? MovementSequence,
    StockMovementType MovementType,
    decimal Quantity,
    decimal? UnitPurchasePrice = null);

public sealed record SupplierDeliveryRecord(
    string DeliveryRecordKey,
    string PurchaseOrderId,
    string SupplierId,
    string SupplierName,
    string ItemId,
    DateOnly PromisedDeliveryDate,
    DateOnly? ActualDeliveryDate);

public sealed record RequisitionRecord(
    string RequisitionId,
    string Department,
    DateOnly RequisitionDate,
    string Description);
