using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", true),
    new(2, "Bai tap da dong", false)
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));
app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    SubmissionDto? submission;
    lock (submissionsLock)
    {
        submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    }

    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Luot nop khong ton tai."))
        : Results.Ok(submission);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId,
    CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Bai tap khong ton tai."));
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_REQUEST",
            "studentCode va repositoryUrl la bat buoc."));
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

    _ = SimulateGradingAsync(
        submission.Id,
        submission.RepositoryUrl,
        submissions,
        submissionsLock,
        hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

static async Task SimulateGradingAsync(
    Guid id,
    string repositoryUrl,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));
    var finalStatus = repositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        ? "graded"
        : "failed";
    await UpdateStatusAsync(id, finalStatus, list, gate, hub);
}

static async Task UpdateStatusAsync(
    Guid id,
    string status,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)
    {
        var index = list.FindIndex(submission => submission.Id == id);
        if (index >= 0)
        {
            updated = list[index] with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            list[index] = updated;
        }
    }

    if (updated is null)
    {
        return;
    }

    var message = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    var groupName = SubmissionGroups.ForSubmission(id);
    Console.WriteLine($"[Push] {groupName} -> {status}");
    await hub.Clients.Group(groupName).SendAsync("SubmissionStatusChanged", message);
}

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        if (!Guid.TryParse(submissionId, out var id))
        {
            throw new HubException("submissionId phai la GUID hop le.");
        }

        var groupName = SubmissionGroups.ForSubmission(id);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {groupName}");
        return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
