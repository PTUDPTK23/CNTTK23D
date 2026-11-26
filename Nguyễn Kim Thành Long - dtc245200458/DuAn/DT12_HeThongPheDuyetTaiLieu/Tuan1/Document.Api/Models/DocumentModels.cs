namespace Document.Api.Models;

public enum DocumentStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected
}

public sealed record DocumentItem(
    Guid Id,
    string Title,
    string Content,
    string Creator,
    string Approver,
    DocumentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DocumentHistory(
    Guid Id,
    Guid DocumentId,
    string Action,
    string PerformedBy,
    string Note,
    DateTimeOffset Timestamp);

public sealed record CreateDocumentRequest(
    string Title,
    string Content,
    string Creator,
    string Approver);

public sealed record DocumentDetailResponse(
    Guid Id,
    string Title,
    string Content,
    string Creator,
    string Approver,
    DocumentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DocumentHistory> History);
