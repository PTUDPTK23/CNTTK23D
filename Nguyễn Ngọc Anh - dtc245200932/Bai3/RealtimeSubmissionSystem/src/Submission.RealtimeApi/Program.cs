using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bài tập 1: Realtime Submission System")
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/api/assignments", () =>
{
    return Results.Ok(assignments);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest req, IHubContext<SubmissionHub> hubContext) =>
    {
        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(
            Guid.NewGuid(),
            assignmentId,
            req.StudentCode,
            req.RepositoryUrl,
            "received",
            now,
            now
        );

        lock (submissionsLock)
        {
            submissions.Add(submission);
        }

        // Khởi động chấm bài ở nền, KHÔNG chờ
        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Không tìm thấy bài nộp."))
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
    lock (gate) // (1) cập nhật dữ liệu
    {
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0)
        {
            updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            list[i] = updated;
        }
    }

    if (updated is null) return;

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt); // (2) dữ liệu tối thiểu
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg); // (3) đẩy đúng nhóm
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

record AssignmentDto(int Id, string Title);

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);

record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

record ApiError(string Code, string Message);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);