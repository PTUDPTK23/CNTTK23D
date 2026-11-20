using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new[]
{
    new AssignmentDto(1, "Bai tap REST API", "Tao va goi REST API", true),
    new AssignmentDto(2, "Bai tap Client", "Viet chuong trinh Client", true),
    new AssignmentDto(3, "Bai tap da dong", "Dung de thu loi bai tap dong", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) =>
    {
        var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);

        if (assignment is null)
            return Results.NotFound(new ApiError(
                "ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError(
                "ASSIGNMENT_CLOSED", "Bai tap da dong."));

        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
            return Results.BadRequest(new ApiError(
                "INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

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
            submission.Id, submissions, submissionsLock, hubContext);

        return Results.Created(
            $"/api/submissions/{submission.Id}",
            submission);
    });

app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
    {
        lock (submissionsLock)
        {
            var submission = submissions.FirstOrDefault(
                s => s.Id == submissionId);

            return submission is null
                ? Results.NotFound(new ApiError(
                    "SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
                : Results.Ok(submission);
        }
    });

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

static async Task SimulateGradingAsync(
    Guid id,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
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
        var index = list.FindIndex(s => s.Id == id);

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

    if (updated is null) return;

    var message = new SubmissionStatusMessage(
        updated.Id, updated.Status, updated.UpdatedAt);

    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", message);

    Console.WriteLine($"[Push] {id} -> {status}");
}

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

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);

record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record SubmissionStatusMessage(
    Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);

record ApiError(string Code, string Message);