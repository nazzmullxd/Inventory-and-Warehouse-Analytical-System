using Iwas.Model.Contracts.Common;

namespace Iwas.Model.Contracts.Results;

public enum AvailabilityStatus { Available, Unavailable, NotApplicable }

public sealed record AvailableValue<T>(T? Value, AvailabilityStatus Status, string? Reason = null)
{
    public static AvailableValue<T> Available(T value) => new(value, AvailabilityStatus.Available);
    public static AvailableValue<T> Unavailable(string reason) => new(default, AvailabilityStatus.Unavailable, reason);
    public static AvailableValue<T> NotApplicable(string reason) => new(default, AvailabilityStatus.NotApplicable, reason);
}

public sealed record AnalysisEnvelope<T>(
    T? Data,
    ReadContextMetadata ReadContext,
    DateTimeOffset GeneratedAtUtc,
    string BusinessTimeZone,
    IReadOnlyList<AnalysisIssue> Issues,
    IReadOnlyList<string> Assumptions)
{
    public bool IsComplete => Data is not null && Issues.All(x => x.Severity != IssueSeverity.Blocking);
}

public sealed record BulkResult<T>(
    IReadOnlyList<T> Rows,
    IReadOnlyList<AnalysisIssue> RowIssues,
    int EvaluatedCount,
    int SuccessfulCount,
    int FailedCount,
    int PageNumber,
    int PageSize)
{
    public int TotalCount => SuccessfulCount + FailedCount;
    public int ReturnedCount => Rows.Count + RowIssues.Count;
    public bool IsComplete => FailedCount == 0;
}

public sealed record DashboardMetric(
    string MetricCode,
    AvailableValue<decimal> Value,
    string Unit,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? PermittedDestination,
    bool IsComplete);
