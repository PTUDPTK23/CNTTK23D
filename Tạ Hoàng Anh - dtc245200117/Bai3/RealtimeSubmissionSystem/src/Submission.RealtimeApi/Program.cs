using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", true),
    new(3, "Bai tap da dong", "Bai tap khong con nhan bai nop.", false)
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
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    var studentCode = request.StudentCode.Trim();
    SubmissionDto submission;
    lock (submissionsLock)
    {
        var duplicated = submissions.Any(item =>
            item.AssignmentId == assignmentId &&
            item.StudentCode.Equals(studentCode, StringComparison.OrdinalIgnoreCase));
        if (duplicated)
            return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));

        var now = DateTimeOffset.UtcNow;
        submission = new SubmissionDto(
            Guid.NewGuid(), assignmentId, studentCode, request.RepositoryUrl.Trim(),
            "received", now, now);
        submissions.Add(submission);
    }

    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext, app.Logger);
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

app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
        if (submission is null)
            return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));

        submissions.Remove(submission);
        return Results.NoContent();
    }
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

static async Task SimulateGradingAsync(Guid submissionId, List<SubmissionDto> submissions,
    object submissionsLock, IHubContext<SubmissionHub> hubContext, ILogger logger)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(submissionId, "processing", submissions, submissionsLock, hubContext, logger);

    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(submissionId, "graded", submissions, submissionsLock, hubContext, logger);
}

static async Task UpdateStatusAsync(Guid submissionId, string status, List<SubmissionDto> submissions,
    object submissionsLock, IHubContext<SubmissionHub> hubContext, ILogger logger)
{
    SubmissionDto updated;
    lock (submissionsLock)
    {
        var index = submissions.FindIndex(submission => submission.Id == submissionId);
        if (index < 0)
            return;

        updated = submissions[index] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
        submissions[index] = updated;
    }

    var message = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    try
    {
        await hubContext.Clients.Group(SubmissionGroups.ForSubmission(submissionId))
            .SendAsync("SubmissionStatusChanged", message);
    }
    catch (Exception exception)
    {
        logger.LogWarning(exception, "Could not send status {Status} for submission {SubmissionId}",
            status, submissionId);
    }
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record ApiError(string Code, string Message);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);

record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());

    public static string ForSubmission(string id) => $"submission:{id}";
}
