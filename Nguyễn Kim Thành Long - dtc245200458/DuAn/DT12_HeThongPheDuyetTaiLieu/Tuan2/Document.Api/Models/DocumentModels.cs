using System.ComponentModel.DataAnnotations;

namespace Document.Api.Models;

public enum DocumentStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected
}

public sealed class DocumentEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Creator { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Approver { get; set; } = "Ban Giám Đốc";

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    [ConcurrencyCheck]
    public int Version { get; set; } = 1;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<DocumentHistoryEntity> History { get; set; } = [];
}

public sealed class DocumentHistoryEntity
{
    [Key]
    public Guid HistoryId { get; set; }

    public Guid DocumentId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public DocumentStatus? FromStatus { get; set; }

    public DocumentStatus ToStatus { get; set; }

    [Required]
    [MaxLength(100)]
    public string PerformedBy { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Note { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    public DocumentEntity? Document { get; set; }
}

public sealed record CreateDocumentRequest(
    string Title,
    string Content,
    string? CreatedBy = null,
    string? Creator = null,
    string? Approver = null,
    string? IdempotencyKey = null);

public sealed record TransitionDocumentRequest(
    string? PerformedBy = null,
    string? Note = null,
    int? ExpectedVersion = null,
    string? IdempotencyKey = null);

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
