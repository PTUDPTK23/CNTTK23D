using Document.Api.Models;

namespace Document.Api.Services;

public enum OperationErrorKind
{
    None,
    NotFound,
    Validation,
    Conflict
}

public sealed record OperationResult<T>(
    bool IsSuccess,
    T? Value,
    OperationErrorKind ErrorKind = OperationErrorKind.None,
    string? ErrorMessage = null)
{
    public static OperationResult<T> Success(T value) => new(true, value);
    public static OperationResult<T> NotFound(string message) => new(false, default, OperationErrorKind.NotFound, message);
    public static OperationResult<T> Validation(string message) => new(false, default, OperationErrorKind.Validation, message);
    public static OperationResult<T> Conflict(string message) => new(false, default, OperationErrorKind.Conflict, message);
}

public interface IDocumentService
{
    Task<OperationResult<DocumentDto>> CreateAsync(CreateDocumentRequest request, string? idempotencyKey = null, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentDto>> GetAllAsync(DocumentStatus? status = null, string? approver = null, CancellationToken ct = default);
    Task<DocumentDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<DocumentDto>> SubmitAsync(Guid id, TransitionDocumentRequest request, string? idempotencyKey = null, CancellationToken ct = default);
    Task<OperationResult<DocumentDto>> ApproveAsync(Guid id, TransitionDocumentRequest request, string? idempotencyKey = null, CancellationToken ct = default);
    Task<OperationResult<DocumentDto>> RejectAsync(Guid id, TransitionDocumentRequest request, string? idempotencyKey = null, CancellationToken ct = default);
}
