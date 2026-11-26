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
    string Creator,
    string Approver);

public sealed record DocumentItemDto(
    Guid Id,
    string Title,
    string Content,
    string Creator,
    string Approver,
    DocumentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DocumentHistoryDto(
    Guid Id,
    Guid DocumentId,
    string Action,
    string PerformedBy,
    string Note,
    DateTimeOffset Timestamp);

public sealed record DocumentDetailDto(
    Guid Id,
    string Title,
    string Content,
    string Creator,
    string Approver,
    DocumentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DocumentHistoryDto> History);
