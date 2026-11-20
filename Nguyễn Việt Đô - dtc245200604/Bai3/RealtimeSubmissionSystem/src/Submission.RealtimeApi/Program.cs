using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();
var enableFailed = app.Configuration.GetValue<bool>("EnableFailed");
if (string.IsNullOrWhiteSpace(app.Configuration["urls"]))
    app.Urls.Add("http://localhost:5000");

app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

var gate = new object();
var assignments = new[]
{
    new AssignmentDto(1, "Bai tap REST API", "Thiet ke endpoint RESTful", true),
    new AssignmentDto(2, "Bai tap Client", "Viet console client HttpClient", true),
    new AssignmentDto(3, "Bai tap da dong", "Bai tap het han nop", false)
};

app.MapGet("/api/assignments", () =>
{
    lock (gate) return Results.Ok(assignments.ToArray());
});
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    lock (gate)
    {
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
        return assignment is null
            ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
            : Results.Ok(assignment);
    }
});

var submissions = new Dictionary<Guid, SubmissionDto>();
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest input, IHubContext<SubmissionHub> hub) =>
{
    lock (gate)
    {
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
        if (string.IsNullOrWhiteSpace(input.StudentCode) || string.IsNullOrWhiteSpace(input.RepositoryUrl))
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

        var studentCode = input.StudentCode.Trim();
        if (submissions.Values.Any(item => item.AssignmentId == assignmentId &&
            string.Equals(item.StudentCode, studentCode, StringComparison.OrdinalIgnoreCase)))
            return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(Guid.NewGuid(), assignmentId,
            studentCode, input.RepositoryUrl.Trim(), "received", now, now);
        submissions.Add(submission.Id, submission);
        _ = SimulateGradingAsync(submission, hub);
        return Results.Created($"/api/submissions/{submission.Id}", submission);
    }
});
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (gate)
    {
        return submissions.TryGetValue(submissionId, out var submission)
            ? Results.Ok(submission)
            : Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay bai nop."));
    }
});

app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (gate)
    {
        return submissions.Remove(submissionId)
            ? Results.NoContent()
            : Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay bai nop."));
    }
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

async Task SimulateGradingAsync(SubmissionDto submission, IHubContext<SubmissionHub> hub)
{
    var stopping = app.Lifetime.ApplicationStopping;
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(1), stopping);
        await UpdateStatusAsync(submission.Id, "processing", hub);
        await Task.Delay(TimeSpan.FromSeconds(2), stopping);
        var result = enableFailed && !submission.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "failed" : "graded";
        await UpdateStatusAsync(submission.Id, result, hub);
    }
    catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
}

async Task UpdateStatusAsync(Guid id, string status, IHubContext<SubmissionHub> hub)
{
    SubmissionDto updated;
    lock (gate)
    {
        if (!submissions.TryGetValue(id, out var submission)) return;
        updated = submissions[id] = submission with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
    }

    var group = SubmissionGroups.ForSubmission(id);
    try
    {
        await hub.Clients.Group(group).SendAsync("SubmissionStatusChanged",
            new SubmissionStatusMessage(id, updated.Status, updated.UpdatedAt));
        app.Logger.LogInformation("[Push] {Group} -> {Status}", group, updated.Status);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "SignalR delivery failed for {Group}; REST still has {Status}", group, status);
    }
}

record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record ApiError(string Code, string Message);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);

sealed class SubmissionHub(ILogger<SubmissionHub> logger) : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        if (!Guid.TryParse(submissionId, out var id)) throw new HubException("Invalid submission id.");
        var group = SubmissionGroups.ForSubmission(id);
        // shortcut: demo has no authentication; check ownership before joining in a real deployment.
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        logger.LogInformation("[Hub] {ConnectionId} theo doi {Group}", Context.ConnectionId, group);
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => $"submission:{id}";
}
