using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Du lieu mau trong RAM: bai 1 va 2 dang mo, bai 3 da dong.
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Xay dung API theo chuan RESTful.", true),
    new(2, "Bai tap Client", "Su dung HttpClient de goi API.", true),
    new(3, "Bai tap nang cao", "Bai tap da het han nop.", false)
};

var submissions = new ConcurrentDictionary<Guid, SubmissionDto>();

app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running",
    port = 5000
}));

// 1. Danh sach bai tap
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 2. Chi tiet bai tap
app.MapGet("/api/assignments/{id:int}", (int id) =>
{
    var assignment = assignments.FirstOrDefault(x => x.Id == id);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap co id {id}."))
        : Results.Ok(assignment);
});

// 3. Tao luot nop bai
app.MapPost("/api/assignments/{id:int}/submissions", (int id, CreateSubmissionRequest? request) =>
{
    var assignment = assignments.FirstOrDefault(x => x.Id == id);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap co id {id}."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong the nop bai."));

    if (request is null ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    if (!Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out var repoUri) ||
        (repoUri.Scheme != Uri.UriSchemeHttp && repoUri.Scheme != Uri.UriSchemeHttps))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "repositoryUrl phai la URL http hoac https hop le."));

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignment.Id,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    submissions[submission.Id] = submission;
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 4. Doc lai mot luot nop
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    return submissions.TryGetValue(submissionId, out var submission)
        ? Results.Ok(submission)
        : Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", $"Khong tim thay luot nop {submissionId}."));
});

app.Run();

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset SubmittedAt);
record ApiError(string Code, string Message);
