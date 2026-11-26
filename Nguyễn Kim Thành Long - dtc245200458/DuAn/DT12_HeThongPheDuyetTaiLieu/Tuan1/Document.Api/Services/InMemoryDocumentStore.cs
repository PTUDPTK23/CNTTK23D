using System.Collections.Concurrent;
using Document.Api.Models;

namespace Document.Api.Services;

public sealed class InMemoryDocumentStore : IDocumentStore
{
    private readonly ConcurrentDictionary<Guid, DocumentItem> _documents = new();
    private readonly ConcurrentDictionary<Guid, List<DocumentHistory>> _histories = new();

    public InMemoryDocumentStore()
    {
        SeedSampleData();
    }

    public DocumentItem Create(CreateDocumentRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var document = new DocumentItem(
            Id: Guid.NewGuid(),
            Title: request.Title.Trim(),
            Content: request.Content.Trim(),
            Creator: request.Creator.Trim(),
            Approver: request.Approver.Trim(),
            Status: DocumentStatus.Draft,
            CreatedAt: now,
            UpdatedAt: now);

        var historyEntry = new DocumentHistory(
            Id: Guid.NewGuid(),
            DocumentId: document.Id,
            Action: "Created",
            PerformedBy: document.Creator,
            Note: "Tài liệu được khởi tạo ở trạng thái Draft.",
            Timestamp: now);

        _documents[document.Id] = document;
        _histories[document.Id] = [historyEntry];

        return document;
    }

    public IReadOnlyList<DocumentItem> GetAll(DocumentStatus? status = null, string? approver = null)
    {
        IEnumerable<DocumentItem> query = _documents.Values;

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(approver))
        {
            var trimmedApprover = approver.Trim();
            query = query.Where(d => string.Equals(d.Approver, trimmedApprover, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(d => d.CreatedAt)
            .ToList();
    }

    public DocumentDetailResponse? GetById(Guid id)
    {
        if (!_documents.TryGetValue(id, out var document))
        {
            return null;
        }

        IReadOnlyList<DocumentHistory> historySnapshot = [];
        if (_histories.TryGetValue(id, out var historyList))
        {
            lock (historyList)
            {
                historySnapshot = historyList
                    .OrderBy(h => h.Timestamp)
                    .ToList();
            }
        }

        return new DocumentDetailResponse(
            Id: document.Id,
            Title: document.Title,
            Content: document.Content,
            Creator: document.Creator,
            Approver: document.Approver,
            Status: document.Status,
            CreatedAt: document.CreatedAt,
            UpdatedAt: document.UpdatedAt,
            History: historySnapshot);
    }

    private void SeedSampleData()
    {
        var baseTime = DateTimeOffset.UtcNow.AddHours(-2);

        var doc1 = new DocumentItem(
            Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Title: "Đề xuất mua sắm thiết bị phòng lab",
            Content: "Kính trình phê duyệt kinh phí trang bị 10 máy tính phục vụ thực hành.",
            Creator: "nguyen.van.a",
            Approver: "tran.thi.b",
            Status: DocumentStatus.Draft,
            CreatedAt: baseTime,
            UpdatedAt: baseTime);

        var doc2 = new DocumentItem(
            Id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Title: "Kế hoạch bảo trì hệ thống máy chủ Q4",
            Content: "Lịch bảo trì định kỳ cụm máy chủ vào cuối tuần.",
            Creator: "le.van.c",
            Approver: "pham.minh.d",
            Status: DocumentStatus.Draft,
            CreatedAt: baseTime.AddMinutes(30),
            UpdatedAt: baseTime.AddMinutes(30));

        _documents[doc1.Id] = doc1;
        _histories[doc1.Id] =
        [
            new DocumentHistory(
                Id: Guid.NewGuid(),
                DocumentId: doc1.Id,
                Action: "Created",
                PerformedBy: doc1.Creator,
                Note: "Dữ liệu mẫu khởi tạo ở trạng thái Draft.",
                Timestamp: doc1.CreatedAt)
        ];

        _documents[doc2.Id] = doc2;
        _histories[doc2.Id] =
        [
            new DocumentHistory(
                Id: Guid.NewGuid(),
                DocumentId: doc2.Id,
                Action: "Created",
                PerformedBy: doc2.Creator,
                Note: "Dữ liệu mẫu khởi tạo ở trạng thái Draft.",
                Timestamp: doc2.CreatedAt)
        ];
    }
}
