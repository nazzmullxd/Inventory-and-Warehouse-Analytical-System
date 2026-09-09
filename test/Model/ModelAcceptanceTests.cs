using System.Reflection;
using Iwas.Model.Contracts.Common;
using Iwas.Model.Contracts.Queries;
using Iwas.Model.Data.InMemory;
using Iwas.Model.Data.Validation;
using Iwas.Model.Repositories;
using Iwas.Model.Source;

namespace Iwas.Model.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ModelTestAttribute : Attribute;

public sealed class ModelAcceptanceTests
{
    private static readonly DateOnly Day = new(2026, 7, 8);

    [ModelTest]
    public void Canonical_duplicate_and_orphan_are_reported()
    {
        var items = new[] { Item(" A "), Item("a") };
        var movements = new[] { Movement("M", "missing", Day, 1, StockMovementType.Receipt) };

        var issues = SourceDataValidator.Validate(items, movements, [], [], new());

        Check.Contains(issues, x => x.Code == IssueCodes.DuplicateSourceKey);
        Check.Contains(issues, x => x.Code == IssueCodes.OrphanItemReference);
    }

    [ModelTest]
    public async Task Stock_and_issue_windows_are_inclusive_and_ordered()
    {
        var snapshot = ValidSnapshot(stockMovements:
        [
            Movement("M3", "I1", Day, 3, StockMovementType.Issue),
            Movement("M1", "I1", Day.AddDays(-2), 1, StockMovementType.Receipt),
            Movement("M2", "I1", Day.AddDays(-1), 2, StockMovementType.Issue),
            Movement("future", "I1", Day.AddDays(1), 1, StockMovementType.Issue)
        ]);
        await using var context = await new InMemoryWarehouseReadStore(snapshot).OpenReadContextAsync();

        var history = await context.GetStockHistoryAsync(new(["I1"], Day));
        var issues = await context.GetIssueHistoryAsync(new(["I1"], new(Day.AddDays(-1), Day)));

        Check.SequenceEqual(new[] { "M1", "M2", "M3" }, history.Data!.Select(x => x.MovementId));
        Check.SequenceEqual(new[] { "M2", "M3" }, issues.Data!.Select(x => x.MovementId));
    }

    [ModelTest]
    public async Task Paging_is_stable_and_reports_full_total()
    {
        var snapshot = ValidSnapshot(items: [Item("C"), Item("a"), Item("B")]);
        await using var context = await new InMemoryWarehouseReadStore(snapshot).OpenReadContextAsync();

        var page = await context.SearchItemsAsync(new(), new(2, 2));

        Check.Equal(3, page.Data!.TotalCount);
        Check.SequenceEqual(new[] { "C" }, page.Data.Rows.Select(x => x.ItemId));
    }

    [ModelTest]
    public async Task Store_copies_input_and_context_exposes_snapshot_metadata()
    {
        var mutableItems = new List<ItemRecord> { Item("I1") };
        var store = new InMemoryWarehouseReadStore(ValidSnapshot(items: mutableItems));
        mutableItems.Add(Item("I2"));
        await using var context = await store.OpenReadContextAsync();

        var result = await context.SearchItemsAsync(new(), new());

        Check.Equal(1, result.Data!.TotalCount);
        Check.Equal(ConsistencyMode.ImmutableExtract, context.Metadata.ConsistencyMode);
        Check.Equal("test-v1", context.Metadata.SnapshotId);
    }

    [ModelTest]
    public async Task Limits_fail_explicitly_instead_of_truncating()
    {
        var store = new InMemoryWarehouseReadStore(ValidSnapshot(items: [Item("A"), Item("B")]), new(MaxItems: 1));
        await using var context = await store.OpenReadContextAsync();

        var result = await context.GetItemAsync("A");

        Check.False(result.IsComplete);
        Check.Contains(result.Issues, x => x.Code == IssueCodes.AnalysisInputTooLarge);
    }

    [ModelTest]
    public async Task Delivered_order_query_deduplicates_verified_multi_item_lines()
    {
        var promised = Day.AddDays(-2);
        var deliveries = new[]
        {
            new SupplierDeliveryRecord("L1", "PO1", "S1", "Supplier", "I1", promised, Day.AddDays(-1)),
            new SupplierDeliveryRecord("L2", "PO1", "S1", "Supplier", "I2", promised, Day.AddDays(-1))
        };
        var snapshot = ValidSnapshot(items: [Item("I1"), Item("I2")], deliveries: deliveries);
        await using var context = await new InMemoryWarehouseReadStore(snapshot).OpenReadContextAsync();

        var supplierWide = await context.GetDeliveredOrdersAsync(new(new(Day.AddDays(-5), Day)));
        var itemSpecific = await context.GetDeliveredOrdersAsync(new(new(Day.AddDays(-5), Day), ItemId: "I2"));

        Check.Equal(1, supplierWide.Data!.Count);
        Check.Equal("I2", itemSpecific.Data!.Single().ItemId);
    }

    [ModelTest]
    public async Task Cancellation_and_disposal_are_honored()
    {
        var context = await new InMemoryWarehouseReadStore(ValidSnapshot()).OpenReadContextAsync();
        var cancelled = new CancellationToken(true);
        await Check.ThrowsAsync<OperationCanceledException>(async () => await context.GetItemAsync("I1", cancelled));
        await context.DisposeAsync();
        await Check.ThrowsAsync<ObjectDisposedException>(async () => await context.GetItemAsync("I1"));
    }

    [ModelTest]
    public void Public_repository_contract_has_no_write_methods()
    {
        var methods = typeof(IWarehouseReadContext).GetMethods().Concat(typeof(IWarehouseReadStore).GetMethods()).Select(x => x.Name).ToArray();
        Check.False(methods.Any(x => x.Contains("Save", StringComparison.OrdinalIgnoreCase)
            || x.Contains("Add", StringComparison.OrdinalIgnoreCase)
            || x.Contains("Update", StringComparison.OrdinalIgnoreCase)
            || x.Contains("Delete", StringComparison.OrdinalIgnoreCase)));
    }

    private static WarehouseSnapshot ValidSnapshot(
        IReadOnlyCollection<ItemRecord>? items = null,
        IReadOnlyCollection<StockMovementRecord>? stockMovements = null,
        IReadOnlyCollection<SupplierDeliveryRecord>? deliveries = null) =>
        new(items ?? [Item("I1")], stockMovements ?? [], deliveries ?? [], [], "test-v1", Day.AddYears(-2), true);
    private static ItemRecord Item(string id) => new(id, $"Item {id}", "each", "General", 10, 20, $"Description {id}", 5);
    private static StockMovementRecord Movement(string id, string item, DateOnly date, int sequence, StockMovementType type) =>
        new(id, item, date, sequence, type, 1, type == StockMovementType.Receipt ? 5 : null);
}

public static class Check
{
    public static void True(bool value) { if (!value) throw new Exception("Expected true."); }
    public static void False(bool value) { if (value) throw new Exception("Expected false."); }
    public static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; actual {actual}."); }
    public static void Contains<T>(IEnumerable<T> values, Func<T, bool> predicate) { if (!values.Any(predicate)) throw new Exception("Expected matching item."); }
    public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual) { if (!expected.SequenceEqual(actual)) throw new Exception("Sequences differ."); }
    public static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}.");
    }
}

public static class Program
{
    public static async Task<int> Main()
    {
        var instance = new ModelAcceptanceTests();
        var tests = typeof(ModelAcceptanceTests).GetMethods().Where(x => x.GetCustomAttribute<ModelTestAttribute>() is not null).OrderBy(x => x.Name).ToArray();
        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                var invoked = test.Invoke(instance, null);
                if (invoked is Task task) await task;
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (TargetInvocationException error) { failed++; Console.WriteLine($"FAIL {test.Name}: {error.InnerException?.Message}"); }
        }
        Console.WriteLine($"{tests.Length - failed}/{tests.Length} model acceptance tests passed.");
        return failed == 0 ? 0 : 1;
    }
}
