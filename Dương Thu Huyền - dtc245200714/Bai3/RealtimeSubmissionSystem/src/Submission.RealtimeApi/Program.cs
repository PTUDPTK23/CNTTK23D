using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var assignments = new[]
{
    new AssignmentDto(1, "Bai tap SignalR", "open"),
    new AssignmentDto(2, "Bai tap da dong", "closed")
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));
app.MapGet("/api/assignments", () => Results.Ok(assignments));
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
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
    {
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        if (assignment.Status != "open")
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
            return Results.BadRequest(new ApiError("INVALID_REQUEST", "studentCode va repositoryUrl la bat buoc."));

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(
            Guid.NewGuid(), assignmentId, request.StudentCode.Trim(), request.RepositoryUrl.Trim(),
            "received", now, now);
        lock (submissionsLock)
            submissions.Add(submission);

        // Return 201 immediately; the grading simulation updates the stored resource in the background.
        _ = SimulateGradingAsync(submission, submissions, submissionsLock, hubContext);
        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });
app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

static async Task SimulateGradingAsync(
    SubmissionDto submission, List<SubmissionDto> submissions, object gate,
    IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(submission.Id, "processing", submissions, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    var finalStatus = submission.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        ? "graded"
        : "failed";
    await UpdateStatusAsync(submission.Id, finalStatus, submissions, gate, hub);
}

static async Task UpdateStatusAsync(
    Guid id, string status, List<SubmissionDto> submissions, object gate,
    IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)
    {
        var index = submissions.FindIndex(item => item.Id == id);
        if (index >= 0)
        {
            updated = submissions[index] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            submissions[index] = updated;
        }
    }

    if (updated is null)
        return;

    var message = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", message);
    Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");
}

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        if (!Guid.TryParse(submissionId, out var id))
            throw new HubException("SubmissionId khong hop le.");
        return Groups.AddToGroupAsync(Context.ConnectionId, SubmissionGroups.ForSubmission(id));
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => $"submission:{id:D}";
}

record AssignmentDto(int Id, string Title, string Status);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
