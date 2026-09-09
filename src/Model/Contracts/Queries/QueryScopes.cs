namespace Iwas.Model.Contracts.Queries;

public sealed record DateRange
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start) throw new ArgumentException("End must be on or after start.", nameof(end));
        Start = start;
        End = end;
    }

    public bool Contains(DateOnly value) => value >= Start && value <= End;
}

public sealed record ItemSearch(string? Text = null, string? Category = null);
public sealed record StockHistoryQuery(IReadOnlyCollection<string> ItemIds, DateOnly ThroughDate);
public sealed record IssueHistoryQuery(IReadOnlyCollection<string> ItemIds, DateRange Period);
public sealed record DeliveryQuery(DateRange Period, string? SupplierId = null, string? ItemId = null);
public sealed record OpenDeliveryQuery(DateRange PromisedPeriod, string? SupplierId = null);
public sealed record RequisitionQuery(DateRange Period, string? Department = null);

public sealed record QueryLimits(
    int MaxItems = 10_000,
    int MaxMovements = 250_000,
    int MaxDeliveries = 100_000,
    int MaxRequisitions = 100_000,
    int MaxPageSize = 500,
    int MaxTextLength = 4_000);
