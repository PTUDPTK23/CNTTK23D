using Document.Api.Models;

namespace Document.Api.Services;

public interface IDocumentStore
{
    DocumentItem Create(CreateDocumentRequest request);
    IReadOnlyList<DocumentItem> GetAll(DocumentStatus? status = null, string? approver = null);
    DocumentDetailResponse? GetById(Guid id);
}
