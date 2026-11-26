namespace Document.Client.Models;

public enum DocumentStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected
}

public sealed record CreateDocumentRequestDto(
    string Title,
    string Content,
    string CreatedBy,
    string Approver);

public sealed record TransitionDocumentRequestDto(
    string? PerformedBy = null,
    string? Note = null,
    int? ExpectedVersion = null);

public sealed record DocumentDto(
    Guid Id,
    string Title,
    string Content,
    string Creator,
    string CreatedBy,
    string Approver,
    DocumentStatus Status,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DocumentHistoryDto(
    Guid HistoryId,
    Guid DocumentId,
    string Action,
    DocumentStatus? FromStatus,
    DocumentStatus ToStatus,
    string PerformedBy,
    string Note,
    DateTimeOffset Timestamp);

public sealed record DocumentDetailDto(
    Guid Id,
    string Title,
    string Content,
    string Creator,
    string CreatedBy,
    string Approver,
    DocumentStatus Status,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DocumentHistoryDto> History);

public sealed record ApiErrorDto(string? Error);
