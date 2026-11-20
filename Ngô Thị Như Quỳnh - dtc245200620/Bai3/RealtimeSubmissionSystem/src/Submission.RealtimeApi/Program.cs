using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "open"),
    new(2, "Bai tap da dong", "closed")
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new
{
    service = "SubmissionRealtimeApi",
    status = "running"
}));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapPost(
    "/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) =>
    {
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
        {
            return Results.NotFound(new ApiError(
                "ASSIGNMENT_NOT_FOUND",
                $"Khong tim thay bai tap {assignmentId}."));
        }

        if (assignment.Status == "closed")
        {
            return Results.BadRequest(new ApiError(
                "ASSIGNMENT_CLOSED",
                "Bai tap da dong, khong the nop bai."));
        }

        if (string.IsNullOrWhiteSpace(request.StudentCode)
            || string.IsNullOrWhiteSpace(request.RepositoryUrl)
            || !Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out var repositoryUri)
            || repositoryUri.Scheme is not ("http" or "https"))
        {
            return Results.BadRequest(new ApiError(
                "INVALID_SUBMISSION",
                "studentCode va repositoryUrl hop le la bat buoc."));
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
            submissions,
            submissionsLock,
            hubContext);

        return Results.Created(
            $"/api/submissions/{submission.Id}",
            submission);
    });

app.MapGet(
    "/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
    {
        lock (submissionsLock)
        {
            var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
            return submission is null
                ? Results.NotFound(new ApiError(
                    "SUBMISSION_NOT_FOUND",
                    $"Khong tim thay luot nop {submissionId}."))
                : Results.Ok(submission);
        }
    });

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static async Task SimulateGradingAsync(
    Guid id,
    List<SubmissionDto> submissions,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", submissions, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));

    string finalStatus;
    lock (gate)
    {
        var submission = submissions.FirstOrDefault(item => item.Id == id);
        finalStatus = submission?.RepositoryUrl.StartsWith(
            "https://",
            StringComparison.OrdinalIgnoreCase) == true
            ? "graded"
            : "failed";
    }

    await UpdateStatusAsync(id, finalStatus, submissions, gate, hub);
}

static async Task UpdateStatusAsync(
    Guid id,
    string status,
    List<SubmissionDto> submissions,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;

    lock (gate)
    {
        var index = submissions.FindIndex(item => item.Id == id);
        if (index >= 0)
        {
            updated = submissions[index] with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            submissions[index] = updated;
        }
    }

    if (updated is null)
    {
        return;
    }

    var message = new SubmissionStatusMessage(
        updated.Id,
        updated.Status,
        updated.UpdatedAt);

    var groupName = SubmissionGroups.ForSubmission(id);
    Console.WriteLine($"[Push] {groupName} -> {status}");

    await hub.Clients
        .Group(groupName)
        .SendAsync("SubmissionStatusChanged", message);
}

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        var groupName = SubmissionGroups.ForSubmission(submissionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine(
            $"[Hub] {Context.ConnectionId[..8]} theo doi {groupName}");
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id)
        => ForSubmission(id.ToString());

    public static string ForSubmission(string id)
        => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, string Status);

record CreateSubmissionRequest(
    string? StudentCode,
    string? RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);

record ApiError(string Code, string Message);
