namespace Iwas.Model.Contracts.Common;

public enum IssueSeverity { Informational, Warning, Blocking }

public sealed record AnalysisIssue(
    string Code,
    IssueSeverity Severity,
    string Message,
    string? EntityId = null,
    string? Field = null,
    bool Retryable = false);

public sealed record AnalysisResult<T>(T? Data, IReadOnlyList<AnalysisIssue> Issues)
{
    public bool IsComplete => Data is not null && Issues.All(x => x.Severity != IssueSeverity.Blocking);

    public static AnalysisResult<T> Success(T data, params AnalysisIssue[] issues) => new(data, issues);
    public static AnalysisResult<T> Failure(params AnalysisIssue[] issues) => new(default, issues);
}

public static class IssueCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string EntityNotFound = "entity_not_found";
    public const string SourceSchemaIncompatible = "source_schema_incompatible";
    public const string DuplicateSourceKey = "duplicate_source_key";
    public const string OrphanItemReference = "orphan_item_reference";
    public const string AmbiguousMovementOrder = "ambiguous_movement_order";
    public const string DeliveryIdentityAmbiguous = "delivery_identity_ambiguous";
    public const string AnalysisInputTooLarge = "analysis_input_too_large";
    public const string DependencyUnavailable = "dependency_unavailable";
}

public enum ConsistencyMode { RequestSnapshot, ImmutableExtract, TransactionSnapshot, Unverified }

public sealed record ReadContextMetadata(
    string SourceVersion,
    string MappingVersion,
    DateTimeOffset ReadAtUtc,
    string SnapshotId,
    ConsistencyMode ConsistencyMode,
    DateOnly? ReliableHistoryStart,
    bool HasTrustedOpeningStock,
    string SourceName);

public sealed record PageRequest(int PageNumber = 1, int PageSize = 50)
{
    public int Offset => checked((PageNumber - 1) * PageSize);
}

public sealed record PageResult<T>(
    IReadOnlyList<T> Rows,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int ReturnedCount => Rows.Count;
}

public sealed record QueryResult<T>(
    T? Data,
    ReadContextMetadata Context,
    IReadOnlyList<AnalysisIssue> Issues)
{
    public bool IsComplete => Data is not null && Issues.All(x => x.Severity != IssueSeverity.Blocking);
    public static QueryResult<T> Success(T data, ReadContextMetadata context, params AnalysisIssue[] issues) => new(data, context, issues);
    public static QueryResult<T> Failure(ReadContextMetadata context, params AnalysisIssue[] issues) => new(default, context, issues);
}
