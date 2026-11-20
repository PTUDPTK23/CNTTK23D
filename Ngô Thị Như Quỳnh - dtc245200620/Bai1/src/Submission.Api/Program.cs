using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
var app = builder.Build();

var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tim hieu he thong phan tan"),
    new Assignment(2, "Bai tap 2", "Xay dung RESTful API")
};

var submissions = new ConcurrentDictionary<Guid, SubmissionResult>();

app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapPost("/api/submissions", (SubmissionRequest request) =>
{
    var assignmentExists = assignments.Any(item => item.Id == request.AssignmentId);
    var repositoryUrlIsValid = Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out var repositoryUrl)
        && (repositoryUrl.Scheme == Uri.UriSchemeHttp || repositoryUrl.Scheme == Uri.UriSchemeHttps);

    if (!assignmentExists || string.IsNullOrWhiteSpace(request.StudentCode) || !repositoryUrlIsValid)
    {
        return Results.BadRequest(new { message = "Du lieu nop bai khong hop le." });
    }

    var result = new SubmissionResult(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    submissions[result.Id] = result;
    return Results.Created($"/api/submissions/{result.Id}", result);
});

app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    if (submissions.TryGetValue(id, out var submission))
    {
        return Results.Ok(submission);
    }

    return Results.NotFound(new { message = "Khong tim thay luot nop." });
});

app.Run();

record Assignment(int Id, string Title, string Description);
record SubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);
