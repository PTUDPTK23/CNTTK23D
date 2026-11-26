using System.Text.Json.Serialization;
using Document.Api.Models;
using Document.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/documents", (CreateDocumentRequest request, IDocumentStore store) =>
{
    var errors = ValidateCreateRequest(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var created = store.Create(request);
    return Results.Created($"/documents/{created.Id}", created);
})
.WithName("CreateDocument");

app.MapGet("/documents", (string? status, string? approver, IDocumentStore store) =>
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

    var documents = store.GetAll(parsedStatus, approver);
    return Results.Ok(documents);
})
.WithName("GetDocuments");

app.MapGet("/documents/{id:guid}", (Guid id, IDocumentStore store) =>
{
    var detail = store.GetById(id);
    return detail is not null
        ? Results.Ok(detail)
        : Results.NotFound(new { Error = $"Không tìm thấy tài liệu với Id '{id}'." });
})
.WithName("GetDocumentById");

app.Run();

static Dictionary<string, string[]> ValidateCreateRequest(CreateDocumentRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        errors[nameof(request.Title)] = ["Title không được để trống."];
    }

    if (string.IsNullOrWhiteSpace(request.Content))
    {
        errors[nameof(request.Content)] = ["Content không được để trống."];
    }

    if (string.IsNullOrWhiteSpace(request.Creator))
    {
        errors[nameof(request.Creator)] = ["Creator không được để trống."];
    }

    if (string.IsNullOrWhiteSpace(request.Approver))
    {
        errors[nameof(request.Approver)] = ["Approver không được để trống."];
    }

    return errors;
}
