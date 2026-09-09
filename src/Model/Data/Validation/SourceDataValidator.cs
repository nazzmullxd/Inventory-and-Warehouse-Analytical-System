using Iwas.Model.Contracts.Common;
using Iwas.Model.Contracts.Queries;
using Iwas.Model.Source;

namespace Iwas.Model.Data.Validation;

public static class SourceDataValidator
{
    public static IReadOnlyList<AnalysisIssue> Validate(
        IReadOnlyCollection<ItemRecord> items,
        IReadOnlyCollection<StockMovementRecord> movements,
        IReadOnlyCollection<SupplierDeliveryRecord> deliveries,
        IReadOnlyCollection<RequisitionRecord> requisitions,
        QueryLimits limits)
    {
        var issues = new List<AnalysisIssue>();
        CheckLimit(items.Count, limits.MaxItems, "Items", issues);
        CheckLimit(movements.Count, limits.MaxMovements, "StockMovements", issues);
        CheckLimit(deliveries.Count, limits.MaxDeliveries, "SupplierDeliveries", issues);
        CheckLimit(requisitions.Count, limits.MaxRequisitions, "Requisitions", issues);

        CheckIdentities(items.Select(x => x.ItemId), "ItemId", issues);
        CheckIdentities(movements.Select(x => x.MovementId), "MovementId", issues);
        CheckIdentities(deliveries.Select(x => x.DeliveryRecordKey), "DeliveryRecordKey", issues);
        CheckIdentities(requisitions.Select(x => x.RequisitionId), "RequisitionId", issues);

        var itemIds = items.Select(x => Canonical(x.ItemId)).ToHashSet(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Unit))
                Add(IssueCodes.SourceSchemaIncompatible, "Item name and unit are required.", item.ItemId, null, issues);
            if (item.HoldingCostPerUnitPerYear < 0 || item.OrderingCostPerPurchaseOrder < 0 || item.CatalogueUnitPrice < 0)
                Add(IssueCodes.SourceSchemaIncompatible, "Item costs and prices cannot be negative.", item.ItemId, "Cost", issues);
            if (item.Description.Length > limits.MaxTextLength)
                Add(IssueCodes.SourceSchemaIncompatible, "Item description exceeds the configured text limit.", item.ItemId, "Description", issues);
        }

        foreach (var movement in movements)
        {
            if (!itemIds.Contains(Canonical(movement.ItemId)))
                Add(IssueCodes.OrphanItemReference, "Stock movement refers to an unknown item.", movement.MovementId, "ItemId", issues);
            if (movement.Quantity <= 0)
                Add(IssueCodes.SourceSchemaIncompatible, "Stock movement quantity must be positive.", movement.MovementId, "Quantity", issues);
            if (movement.MovementType == StockMovementType.Receipt && movement.UnitPurchasePrice < 0)
                Add(IssueCodes.SourceSchemaIncompatible, "Receipt price cannot be negative.", movement.MovementId, "UnitPurchasePrice", issues);
        }

        foreach (var group in movements.GroupBy(x => (Canonical(x.ItemId), x.MovementDate)))
        {
            if (group.Count() > 1 && (group.Any(x => x.MovementSequence is null) || group.GroupBy(x => x.MovementSequence).Any(x => x.Count() > 1)))
                Add(IssueCodes.AmbiguousMovementOrder, $"Movement chronology is not proven for {group.Key.MovementDate:yyyy-MM-dd}.", group.Key.Item1, "MovementSequence", issues);
        }

        foreach (var delivery in deliveries)
        {
            if (!itemIds.Contains(Canonical(delivery.ItemId)))
                Add(IssueCodes.OrphanItemReference, "Supplier delivery refers to an unknown item.", delivery.DeliveryRecordKey, "ItemId", issues);
            if (string.IsNullOrWhiteSpace(delivery.SupplierId) || string.IsNullOrWhiteSpace(delivery.SupplierName))
                Add(IssueCodes.SourceSchemaIncompatible, "Supplier identity and name are required.", delivery.DeliveryRecordKey, "SupplierId", issues);
            if (string.IsNullOrWhiteSpace(delivery.PurchaseOrderId))
                Add(IssueCodes.DeliveryIdentityAmbiguous, "A verified purchase-order identity is required.", delivery.DeliveryRecordKey, "PurchaseOrderId", issues);
        }
        foreach (var order in deliveries.Where(x => !string.IsNullOrWhiteSpace(x.PurchaseOrderId)).GroupBy(x => Canonical(x.PurchaseOrderId)))
        {
            var headers = order.Select(x => (Canonical(x.SupplierId), x.PromisedDeliveryDate, x.ActualDeliveryDate)).Distinct().Count();
            if (headers > 1)
                Add(IssueCodes.DeliveryIdentityAmbiguous, "Purchase-order rows have conflicting delivery headers.", order.Key, "PurchaseOrderId", issues);
        }

        foreach (var requisition in requisitions)
        {
            if (string.IsNullOrWhiteSpace(requisition.Department))
                Add(IssueCodes.SourceSchemaIncompatible, "Requisition department is required.", requisition.RequisitionId, "Department", issues);
            if (requisition.Description.Length > limits.MaxTextLength)
                Add(IssueCodes.SourceSchemaIncompatible, "Requisition description exceeds the configured text limit.", requisition.RequisitionId, "Description", issues);
        }

        return issues.OrderBy(x => x.Code, StringComparer.Ordinal).ThenBy(x => x.EntityId, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static string Canonical(string value) => value.Trim().ToUpperInvariant();

    private static void CheckIdentities(IEnumerable<string> identities, string field, ICollection<AnalysisIssue> issues)
    {
        foreach (var entry in identities.Select((value, index) => (value, index)))
            if (string.IsNullOrWhiteSpace(entry.value)) Add(IssueCodes.SourceSchemaIncompatible, $"{field} is required.", $"row:{entry.index}", field, issues);
        foreach (var group in identities.Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(Canonical).Where(x => x.Count() > 1))
            Add(IssueCodes.DuplicateSourceKey, $"Canonical {field} '{group.Key}' occurs more than once.", group.Key, field, issues);
    }
    private static void CheckLimit(int actual, int maximum, string field, ICollection<AnalysisIssue> issues)
    {
        if (actual > maximum) Add(IssueCodes.AnalysisInputTooLarge, $"{field} contains {actual} records; maximum is {maximum}.", null, field, issues);
    }
    private static void Add(string code, string message, string? entity, string? field, ICollection<AnalysisIssue> issues) =>
        issues.Add(new(code, IssueSeverity.Blocking, message, entity, field));
}
