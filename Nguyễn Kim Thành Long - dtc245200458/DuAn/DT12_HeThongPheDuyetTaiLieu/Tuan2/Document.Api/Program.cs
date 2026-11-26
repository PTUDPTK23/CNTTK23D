using System.Text.Json;
using System.Text.Json.Serialization;
using Document.Api.Data;
using Document.Api.Models;
using Document.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    Converters = { new JsonStringEnumConverter() }
};

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "document_approval.db");
builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<IDocumentService, DocumentService>();

var app = builder.Build();

await InitializeDatabaseAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();

// 1. POST /documents - Tạo tài liệu mới ở trạng thái Draft
app.MapPost("/documents", async (
    CreateDocumentRequest request,
    HttpRequest httpRequest,
    IDocumentService service,
    CancellationToken ct) =>
{
    var idempotencyKey = httpRequest.Headers["Idempotency-Key"].FirstOrDefault();
    var result = await service.CreateAsync(request, idempotencyKey, ct);
    return ToHttpResult(result, created: true);
})
.WithName("CreateDocument");

// 2. GET /documents - Lấy danh sách tài liệu (lọc tùy chọn ?status=&approver=)
app.MapGet("/documents", async (
    string? status,
    string? approver,
    IDocumentService service,
    CancellationToken ct) =>
{
    DocumentStatus? parsedStatus = null;
    if (!string.IsNullOrWhiteSpace(status))
    {
        if (!Enum.TryParse<DocumentStatus>(status.Trim(), ignoreCase: true, out var statusValue) ||
            !Enum.IsDefined(statusValue))
        {
            return Results.BadRequest(new
            {
                Error = $"Trạng thái '{status}' không hợp lệ. Các giá trị hỗ trợ: {string.Join(", ", Enum.GetNames<DocumentStatus>())}."
            });
        }

        parsedStatus = statusValue;
    }

    var documents = await service.GetAllAsync(parsedStatus, approver, ct);
    return Results.Ok(documents);
})
.WithName("GetDocuments");

// 3. GET /documents/{id} - Lấy chi tiết tài liệu kèm lịch sử theo trình tự thời gian
app.MapGet("/documents/{id:guid}", async (
    Guid id,
    IDocumentService service,
    CancellationToken ct) =>
{
    var detail = await service.GetByIdAsync(id, ct);
    return detail is not null
        ? Results.Ok(detail)
        : Results.NotFound(new { Error = $"Không tìm thấy tài liệu với Id '{id}'." });
})
.WithName("GetDocumentById");

// 4. POST /documents/{id}/submit - Draft -> Submitted
app.MapPost("/documents/{id:guid}/submit", async (
    Guid id,
    HttpRequest httpRequest,
    IDocumentService service,
    CancellationToken ct) =>
{
    var request = await ReadOptionalJsonAsync<TransitionDocumentRequest>(httpRequest, jsonOptions, ct)
        ?? new TransitionDocumentRequest();
    var idempotencyKey = httpRequest.Headers["Idempotency-Key"].FirstOrDefault();

    var result = await service.SubmitAsync(id, request, idempotencyKey, ct);
    return ToHttpResult(result);
})
.WithName("SubmitDocument");

// 5. POST /documents/{id}/approve - Submitted -> Approved
app.MapPost("/documents/{id:guid}/approve", async (
    Guid id,
    HttpRequest httpRequest,
    IDocumentService service,
    CancellationToken ct) =>
{
    var request = await ReadOptionalJsonAsync<TransitionDocumentRequest>(httpRequest, jsonOptions, ct)
        ?? new TransitionDocumentRequest();
    var idempotencyKey = httpRequest.Headers["Idempotency-Key"].FirstOrDefault();

    var result = await service.ApproveAsync(id, request, idempotencyKey, ct);
    return ToHttpResult(result);
})
.WithName("ApproveDocument");

// 6. POST /documents/{id}/reject - Submitted -> Rejected (bắt buộc có Note)
app.MapPost("/documents/{id:guid}/reject", async (
    Guid id,
    HttpRequest httpRequest,
    IDocumentService service,
    CancellationToken ct) =>
{
    var request = await ReadOptionalJsonAsync<TransitionDocumentRequest>(httpRequest, jsonOptions, ct)
        ?? new TransitionDocumentRequest();
    var idempotencyKey = httpRequest.Headers["Idempotency-Key"].FirstOrDefault();

    var result = await service.RejectAsync(id, request, idempotencyKey, ct);
    return ToHttpResult(result);
})
.WithName("RejectDocument");

app.Run();

static IResult ToHttpResult(OperationResult<DocumentDto> result, bool created = false)
{
    if (result.IsSuccess && result.Value is not null)
    {
        return created
            ? Results.Created($"/documents/{result.Value.Id}", result.Value)
            : Results.Ok(result.Value);
    }

    return result.ErrorKind switch
    {
        OperationErrorKind.NotFound => Results.NotFound(new { Error = result.ErrorMessage }),
        OperationErrorKind.Validation => Results.BadRequest(new { Error = result.ErrorMessage }),
        OperationErrorKind.Conflict => Results.Conflict(new { Error = result.ErrorMessage }),
        _ => Results.BadRequest(new { Error = result.ErrorMessage ?? "Yêu cầu không hợp lệ." })
    };
}

static async Task<T?> ReadOptionalJsonAsync<T>(
    HttpRequest request,
    JsonSerializerOptions options,
    CancellationToken ct)
{
    if (request.ContentLength == 0)
    {
        return default;
    }

    try
    {
        return await JsonSerializer.DeserializeAsync<T>(request.Body, options, ct);
    }
    catch (JsonException)
    {
        return default;
    }
}

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();

    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;");

    if (!await db.Documents.AnyAsync())
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-15);

        var doc1 = new DocumentEntity
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Title = "Đề xuất nâng cấp cụm máy chủ phân tán",
            Content = "Bổ sung 2 node xử lý và cấu hình SQLite WAL cho dịch vụ phê duyệt tài liệu.",
            Creator = "nguyen.kim.thanh.long",
            Approver = "truong.phong.it",
            Status = DocumentStatus.Submitted,
            Version = 2,
            CreatedAt = now,
            UpdatedAt = now.AddMinutes(5)
        };

        var doc2 = new DocumentEntity
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Title = "Kế hoạch kiểm thử tải hệ thống Q4",
            Content = "Kịch bản kiểm thử 500 request đồng thời và xác minh Optimistic Concurrency.",
            Creator = "tran.minh.khoa",
            Approver = "giam.doc.ky.thuat",
            Status = DocumentStatus.Draft,
            Version = 1,
            CreatedAt = now.AddMinutes(2),
            UpdatedAt = now.AddMinutes(2)
        };

        db.Documents.AddRange(doc1, doc2);
        db.DocumentHistories.AddRange(
            new DocumentHistoryEntity
            {
                HistoryId = Guid.NewGuid(),
                DocumentId = doc1.Id,
                Action = "Created",
                FromStatus = null,
                ToStatus = DocumentStatus.Draft,
                PerformedBy = doc1.Creator,
                Note = "Dữ liệu mẫu khởi tạo ở trạng thái Draft.",
                Timestamp = doc1.CreatedAt
            },
            new DocumentHistoryEntity
            {
                HistoryId = Guid.NewGuid(),
                DocumentId = doc1.Id,
                Action = "Submitted",
                FromStatus = DocumentStatus.Draft,
                ToStatus = DocumentStatus.Submitted,
                PerformedBy = doc1.Creator,
                Note = "Đã gửi duyệt để sẵn sàng test Approve / Reject / Race Condition.",
                Timestamp = doc1.UpdatedAt
            },
            new DocumentHistoryEntity
            {
                HistoryId = Guid.NewGuid(),
                DocumentId = doc2.Id,
                Action = "Created",
                FromStatus = null,
                ToStatus = DocumentStatus.Draft,
                PerformedBy = doc2.Creator,
                Note = "Dữ liệu mẫu khởi tạo ở trạng thái Draft.",
                Timestamp = doc2.CreatedAt
            });

        await db.SaveChangesAsync();
    }
}
