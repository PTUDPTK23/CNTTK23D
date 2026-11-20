using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint va xu ly ma trang thai HTTP.", true),
    new(2, "Bai tap Client", "Viet console client goi API.", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

app.MapHub<SubmissionHub>("/hubs/submissions");

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId,
    CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hubContext,
    IHostApplicationLifetime lifetime) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop."));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));
    }

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        now,
        now);

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    _ = GradeSubmissionAsync(submission.Id, hubContext, lifetime.ApplicationStopping);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

app.Run();

async Task GradeSubmissionAsync(
    Guid submissionId,
    IHubContext<SubmissionHub> hubContext,
    CancellationToken cancellationToken)
{
    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    await UpdateSubmissionStatusAsync(submissionId, "grading", hubContext, cancellationToken);

    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
    await UpdateSubmissionStatusAsync(submissionId, "graded", hubContext, cancellationToken);
}

async Task UpdateSubmissionStatusAsync(
    Guid submissionId,
    string status,
    IHubContext<SubmissionHub> hubContext,
    CancellationToken cancellationToken)
{
    SubmissionDto? updatedSubmission;
    lock (submissionsLock)
    {
        var index = submissions.FindIndex(item => item.Id == submissionId);
        if (index < 0)
        {
            return;
        }

        updatedSubmission = submissions[index] with
        {
            Status = status,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        submissions[index] = updatedSubmission;
    }

    await hubContext.Clients
        .Group(SubmissionHub.GroupName(submissionId))
        .SendAsync("SubmissionStatusChanged", updatedSubmission, cancellationToken);
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record ApiError(string Code, string Message);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
