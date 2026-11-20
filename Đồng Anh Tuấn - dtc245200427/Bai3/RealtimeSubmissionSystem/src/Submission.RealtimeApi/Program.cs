using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();
var assignments = new[]
{
    new AssignmentDto(1, "Bai tap SignalR", "Theo doi trang thai luot nop", true),
    new AssignmentDto(2, "Bai tap da dong", "Vi du bai khong con nhan luot nop", false)
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));
app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
    {
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            !Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out var repository) ||
            (repository.Scheme != Uri.UriSchemeHttps && repository.Scheme != Uri.UriSchemeHttp))
        {
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION",
                "Can ma sinh vien va URL repository hop le (http hoac https)."));
        }

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(Guid.NewGuid(), assignmentId,
            request.StudentCode.Trim(), request.RepositoryUrl!.Trim(), "received", now, now);
        lock (submissionsLock)
        {
            submissions.Add(submission);
        }

        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext, app.Logger);
        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    SubmissionDto? submission;
    lock (submissionsLock)
    {
        submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    }

    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> submissions, object gate,
    IHubContext<SubmissionHub> hub, ILogger logger)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", submissions, gate, hub, logger);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", submissions, gate, hub, logger);
}

static async Task UpdateStatusAsync(Guid id, string status, List<SubmissionDto> submissions,
    object gate, IHubContext<SubmissionHub> hub, ILogger logger)
{
    SubmissionDto updated;
    lock (gate)
    {
        var index = submissions.FindIndex(item => item.Id == id);
        if (index < 0)
            return;

        updated = submissions[index] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
        submissions[index] = updated;
    }

    var group = SubmissionGroups.ForSubmission(id);
    var message = new SubmissionStatusMessage(id, updated.Status, updated.UpdatedAt);
    try
    {
        await hub.Clients.Group(group).SendAsync("SubmissionStatusChanged", message);
        Console.WriteLine($"[Push] {group} -> {status}");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Khong gui duoc trang thai {Status} cho {SubmissionId}", status, id);
    }
}

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        if (!Guid.TryParse(submissionId, out var id))
            throw new HubException("Ma luot nop khong hop le.");

        var group = SubmissionGroups.ForSubmission(id);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        Console.WriteLine($"[Hub] {Context.ConnectionId} theo doi {group}");
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
