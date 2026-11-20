using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thuc hanh xay dung RESTful API voi Minimal API.", true),
    new(2, "Bai tap Client", "Thuc hanh HttpClient va xu ly loi HTTP.", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(x => x.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    async (int assignmentId, CreateSubmissionRequest request,
        IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(x => x.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong the nop."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    if (!Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out var repoUri) ||
        (repoUri.Scheme != Uri.UriSchemeHttp && repoUri.Scheme != Uri.UriSchemeHttps))
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION", "RepositoryUrl phai la dia chi HTTP hoac HTTPS hop le."));

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    _ = ProcessSubmissionAsync(submission, hubContext, submissions, submissionsLock);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    SubmissionDto? submission;
    lock (submissionsLock)
    {
        submission = submissions.FirstOrDefault(x => x.Id == submissionId);
    }

    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static async Task ProcessSubmissionAsync(
    SubmissionDto initial,
    IHubContext<SubmissionHub> hubContext,
    List<SubmissionDto> submissions,
    object submissionsLock)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    var nextStatus = "processing";
    await UpdateAndNotifyAsync(initial.Id, nextStatus, hubContext, submissions, submissionsLock);

    await Task.Delay(TimeSpan.FromSeconds(2));
    nextStatus = initial.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        ? "graded"
        : "failed";

    await UpdateAndNotifyAsync(initial.Id, nextStatus, hubContext, submissions, submissionsLock);
}

static async Task UpdateAndNotifyAsync(
    Guid id,
    string status,
    IHubContext<SubmissionHub> hubContext,
    List<SubmissionDto> submissions,
    object submissionsLock)
{
    SubmissionStatusMessage? message = null;

    lock (submissionsLock)
    {
        var index = submissions.FindIndex(x => x.Id == id);
        if (index >= 0)
        {
            var current = submissions[index];
            var updatedAt = DateTimeOffset.UtcNow;
            submissions[index] = current with { Status = status, UpdatedAt = updatedAt };
            message = new SubmissionStatusMessage(id, status, updatedAt);
        }
    }

    if (message is not null)
    {
        await hubContext.Clients
            .Group($"submission:{id}")
            .SendAsync("SubmissionStatusChanged", message);
    }
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? UpdatedAt = null);
record ApiError(string Code, string Message);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
