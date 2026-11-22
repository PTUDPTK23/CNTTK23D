using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();
var assignments = new[]
{
    new AssignmentDto(1, "Bai tap SignalR", true),
    new AssignmentDto(2, "Bai tap REST", false)
};
var submissions = new Dictionary<Guid, SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "EduSubmit", status = "running" }));
app.MapGet("/api/assignments", () => assignments);
app.MapHub<SubmissionHub>("/hubs/submissions");

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        return submissions.TryGetValue(submissionId, out var submission)
            ? Results.Ok(submission)
            : Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));
    }
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hub) =>
    {
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
        if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Can studentCode va repositoryUrl."));

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(Guid.NewGuid(), assignmentId, request.StudentCode.Trim(),
            request.RepositoryUrl.Trim(), "received", now, now);
        lock (submissionsLock)
        {
            submissions.Add(submission.Id, submission);
        }

        _ = SimulateGradingAsync(submission.Id, hub, app.Lifetime.ApplicationStopping);
        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

app.Run();

async Task SimulateGradingAsync(Guid id, IHubContext<SubmissionHub> hub, CancellationToken stopping)
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(1), stopping);
        await UpdateStatusAsync(id, "processing", hub, stopping);
        await Task.Delay(TimeSpan.FromSeconds(2), stopping);
        await UpdateStatusAsync(id, "graded", hub, stopping);
    }
    catch (OperationCanceledException) when (stopping.IsCancellationRequested)
    {
    }
}

async Task UpdateStatusAsync(Guid id, string status, IHubContext<SubmissionHub> hub, CancellationToken stopping)
{
    SubmissionDto updated;
    lock (submissionsLock)
    {
        updated = submissions[id] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
        submissions[id] = updated;
    }

    var group = SubmissionGroups.ForSubmission(id);
    var message = new SubmissionStatusMessage(id, status, updated.UpdatedAt);
    try
    {
        await hub.Clients.Group(group).SendAsync("SubmissionStatusChanged", message, stopping);
        app.Logger.LogInformation("[Push] {Group} -> {Status}", group, status);
    }
    catch (Exception exception) when (!stopping.IsCancellationRequested)
    {
        app.Logger.LogWarning(exception, "Khong the gui trang thai {Status} cho {SubmissionId}", status, id);
    }
}

sealed class SubmissionHub(ILogger<SubmissionHub> logger) : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        if (!Guid.TryParse(submissionId, out var id))
            throw new HubException("Ma luot nop khong hop le.");

        var group = SubmissionGroups.ForSubmission(id);
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
        logger.LogInformation("[Hub] {ConnectionId} theo doi {Group}", Context.ConnectionId, group);
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
