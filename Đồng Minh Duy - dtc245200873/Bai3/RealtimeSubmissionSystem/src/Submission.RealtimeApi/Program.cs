using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Nhan thong bao trang thai bai nop theo thoi gian thuc", true),
    new(2, "Bai tap REST API", "On lai API assignment va submission", true)
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
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong nop bai."));
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac repository."));

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(Guid.NewGuid(), assignmentId, request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(), "received", now, now);
    lock (submissionsLock) submissions.Add(submission);
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

static async Task SimulateGradingAsync(Guid submissionId, List<SubmissionDto> submissions,
    object submissionsLock, IHubContext<SubmissionHub> hubContext)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(submissionId, "processing", submissions, submissionsLock, hubContext);
    await Task.Delay(TimeSpan.FromSeconds(2));
    string finalStatus;
    lock (submissionsLock)
        finalStatus = submissions.FirstOrDefault(item => item.Id == submissionId)?.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true ? "graded" : "failed";
    await UpdateStatusAsync(submissionId, finalStatus, submissions, submissionsLock, hubContext);
}

static async Task UpdateStatusAsync(Guid submissionId, string status, List<SubmissionDto> submissions,
    object submissionsLock, IHubContext<SubmissionHub> hubContext)
{
    SubmissionDto? updated = null;
    lock (submissionsLock)
    {
        var index = submissions.FindIndex(item => item.Id == submissionId);
        if (index >= 0)
        {
            updated = submissions[index] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            submissions[index] = updated;
        }
    }
    if (updated is null) return;
    Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(submissionId)} -> {status}");
    await hubContext.Clients.Group(SubmissionGroups.ForSubmission(submissionId))
        .SendAsync("SubmissionStatusChanged", new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt));
}

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..Math.Min(8, Context.ConnectionId.Length)]} theo doi submission:{submissionId}");
        return Groups.AddToGroupAsync(Context.ConnectionId, SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
