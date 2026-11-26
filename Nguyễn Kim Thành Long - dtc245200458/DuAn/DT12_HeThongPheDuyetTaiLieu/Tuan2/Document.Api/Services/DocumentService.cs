using Document.Api.Data;
using Document.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Document.Api.Services;

public sealed class DocumentService(DocumentDbContext dbContext) : IDocumentService
{
    public async Task<OperationResult<DocumentDto>> CreateAsync(
        CreateDocumentRequest request,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        var creator = (request.CreatedBy ?? request.Creator)?.Trim();
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Content) ||
            string.IsNullOrWhiteSpace(creator))
        {
            return OperationResult<DocumentDto>.Validation("Title, Content và CreatedBy/Creator không được để trống.");
        }

        var approver = string.IsNullOrWhiteSpace(request.Approver)
            ? "Ban Giám Đốc"
            : request.Approver.Trim();

        var effectiveKey = NormalizeKey(idempotencyKey ?? request.IdempotencyKey);
        if (effectiveKey is not null)
        {
            var existingHistory = await dbContext.DocumentHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Action == "Created" && h.IdempotencyKey == effectiveKey, ct);

            if (existingHistory is not null)
            {
                var existingDoc = await dbContext.Documents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == existingHistory.DocumentId, ct);

                if (existingDoc is not null)
                {
                    return OperationResult<DocumentDto>.Success(ToDto(existingDoc));
                }
            }
        }

        var now = DateTimeOffset.UtcNow;
        var document = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Creator = creator,
            Approver = approver,
            Status = DocumentStatus.Draft,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        var history = new DocumentHistoryEntity
        {
            HistoryId = Guid.NewGuid(),
            DocumentId = document.Id,
            Action = "Created",
            FromStatus = null,
            ToStatus = DocumentStatus.Draft,
            PerformedBy = creator,
            Note = "Tài liệu được khởi tạo ở trạng thái Draft.",
            Timestamp = now,
            IdempotencyKey = effectiveKey
        };

        dbContext.Documents.Add(document);
        dbContext.DocumentHistories.Add(history);
        await dbContext.SaveChangesAsync(ct);

        return OperationResult<DocumentDto>.Success(ToDto(document));
    }

    public async Task<IReadOnlyList<DocumentDto>> GetAllAsync(
        DocumentStatus? status = null,
        string? approver = null,
        CancellationToken ct = default)
    {
        var query = dbContext.Documents.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(approver))
        {
            var trimmedApprover = approver.Trim().ToLower();
            query = query.Where(d => d.Approver.ToLower() == trimmedApprover);
        }

        var entities = await query.ToListAsync(ct);

        return entities
            .OrderByDescending(d => d.CreatedAt)
            .Select(ToDto)
            .ToList();
    }

    public async Task<DocumentDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await dbContext.Documents
            .AsNoTracking()
            .Include(d => d.History)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (entity is null)
        {
            return null;
        }

        var historyDtos = entity.History
            .OrderBy(h => h.Timestamp)
            .Select(h => new DocumentHistoryDto(
                h.HistoryId,
                h.DocumentId,
                h.Action,
                h.FromStatus,
                h.ToStatus,
                h.PerformedBy,
                h.Note,
                h.Timestamp))
            .ToList();

        return new DocumentDetailDto(
            entity.Id,
            entity.Title,
            entity.Content,
            entity.Creator,
            entity.Creator,
            entity.Approver,
            entity.Status,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt,
            historyDtos);
    }

    public Task<OperationResult<DocumentDto>> SubmitAsync(
        Guid id,
        TransitionDocumentRequest request,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        return TransitionAsync(
            id: id,
            expectedCurrentStatus: DocumentStatus.Draft,
            targetStatus: DocumentStatus.Submitted,
            actionName: "Submitted",
            defaultPerformedBy: doc => doc.Creator,
            defaultNote: "Tài liệu đã được gửi đi để chờ phê duyệt.",
            requireNote: false,
            request: request,
            idempotencyKey: idempotencyKey,
            ct: ct);
    }

    public Task<OperationResult<DocumentDto>> ApproveAsync(
        Guid id,
        TransitionDocumentRequest request,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        return TransitionAsync(
            id: id,
            expectedCurrentStatus: DocumentStatus.Submitted,
            targetStatus: DocumentStatus.Approved,
            actionName: "Approved",
            defaultPerformedBy: doc => doc.Approver,
            defaultNote: "Tài liệu đã được phê duyệt.",
            requireNote: false,
            request: request,
            idempotencyKey: idempotencyKey,
            ct: ct);
    }

    public Task<OperationResult<DocumentDto>> RejectAsync(
        Guid id,
        TransitionDocumentRequest request,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        return TransitionAsync(
            id: id,
            expectedCurrentStatus: DocumentStatus.Submitted,
            targetStatus: DocumentStatus.Rejected,
            actionName: "Rejected",
            defaultPerformedBy: doc => doc.Approver,
            defaultNote: string.Empty,
            requireNote: true,
            request: request,
            idempotencyKey: idempotencyKey,
            ct: ct);
    }

    private async Task<OperationResult<DocumentDto>> TransitionAsync(
        Guid id,
        DocumentStatus expectedCurrentStatus,
        DocumentStatus targetStatus,
        string actionName,
        Func<DocumentEntity, string> defaultPerformedBy,
        string defaultNote,
        bool requireNote,
        TransitionDocumentRequest request,
        string? idempotencyKey,
        CancellationToken ct)
    {
        if (requireNote && string.IsNullOrWhiteSpace(request.Note))
        {
            return OperationResult<DocumentDto>.Validation("Bắt buộc phải nhập lý do từ chối trong trường Note.");
        }

        var effectiveKey = NormalizeKey(idempotencyKey ?? request.IdempotencyKey);
        if (effectiveKey is not null)
        {
            var existingHistory = await dbContext.DocumentHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.DocumentId == id && h.IdempotencyKey == effectiveKey, ct);

            if (existingHistory is not null)
            {
                var currentDoc = await dbContext.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
                if (currentDoc is not null)
                {
                    return OperationResult<DocumentDto>.Success(ToDto(currentDoc));
                }
            }
        }

        var document = await dbContext.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (document is null)
        {
            return OperationResult<DocumentDto>.NotFound($"Không tìm thấy tài liệu với Id '{id}'.");
        }

        if (request.ExpectedVersion.HasValue && document.Version != request.ExpectedVersion.Value)
        {
            return OperationResult<DocumentDto>.Conflict(
                $"Tài liệu đã được xử lý bởi người khác hoặc trạng thái đã thay đổi (Version hiện tại: {document.Version}, yêu cầu: {request.ExpectedVersion.Value}).");
        }

        if (document.Status != expectedCurrentStatus)
        {
            return OperationResult<DocumentDto>.Conflict(
                $"Không thể thực hiện '{actionName}' khi tài liệu đang ở trạng thái '{document.Status}'. Yêu cầu trạng thái '{expectedCurrentStatus}'.");
        }

        var now = DateTimeOffset.UtcNow;
        var performedBy = string.IsNullOrWhiteSpace(request.PerformedBy)
            ? defaultPerformedBy(document)
            : request.PerformedBy.Trim();
        var note = string.IsNullOrWhiteSpace(request.Note)
            ? defaultNote
            : request.Note.Trim();

        var fromStatus = document.Status;
        document.Status = targetStatus;
        document.Version += 1;
        document.UpdatedAt = now;

        var history = new DocumentHistoryEntity
        {
            HistoryId = Guid.NewGuid(),
            DocumentId = document.Id,
            Action = actionName,
            FromStatus = fromStatus,
            ToStatus = targetStatus,
            PerformedBy = performedBy,
            Note = note,
            Timestamp = now,
            IdempotencyKey = effectiveKey
        };

        dbContext.DocumentHistories.Add(history);

        try
        {
            await dbContext.SaveChangesAsync(ct);
            return OperationResult<DocumentDto>.Success(ToDto(document));
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<DocumentDto>.Conflict(
                "Tài liệu đã được xử lý bởi người khác hoặc trạng thái đã thay đổi.");
        }
    }

    private static string? NormalizeKey(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : key.Trim();

    private static DocumentDto ToDto(DocumentEntity entity) =>
        new(
            entity.Id,
            entity.Title,
            entity.Content,
            entity.Creator,
            entity.Creator,
            entity.Approver,
            entity.Status,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
}
