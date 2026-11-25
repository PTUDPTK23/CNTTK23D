using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

// Du lieu mau duoc luu trong bo nho cua tien trinh API.
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Theo doi trang thai nop bai theo thoi gian thuc.", true),
    new(2, "Bai tap Client", "Viet console client goi API bang HttpClient.", true),
    new(3, "Bai tap da dong", "Dung de kiem tra loi ASSIGNMENT_CLOSED.", false)
};

var submissions = new System.Collections.Concurrent.ConcurrentDictionary<Guid, SubmissionDto>();

app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);

    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    return Results.Ok(assignment);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hub) =>
    {
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);

        // Kiem tra dung thu tu: ton tai -> con mo -> du lieu hop le.
        if (assignment is null)
        {
            return Results.NotFound(new ApiError(
                "ASSIGNMENT_NOT_FOUND",
                "Khong tim thay bai tap."));
        }

        if (!assignment.IsOpen)
        {
            return Results.BadRequest(new ApiError(
                "ASSIGNMENT_CLOSED",
                "Bai tap da dong, khong the nop bai."));
        }

        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new ApiError(
                "INVALID_SUBMISSION",
                "Thieu ma sinh vien hoac dia chi kho ma nguon."));
        }

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(
            Id: Guid.NewGuid(),
            AssignmentId: assignmentId,
            StudentCode: request.StudentCode.Trim(),
            RepositoryUrl: request.RepositoryUrl.Trim(),
            Status: "received",
            ReceivedAt: now,
            UpdatedAt: now);

        submissions[submission.Id] = submission;
        _ = GradeAsync(submission, hub, app.Logger, app.Lifetime.ApplicationStopping);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    if (!submissions.TryGetValue(submissionId, out var submission))
    {
        return Results.NotFound(new ApiError(
            "SUBMISSION_NOT_FOUND",
            "Khong tim thay luot nop."));
    }

    return Results.Ok(submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

async Task GradeAsync(SubmissionDto submission, IHubContext<SubmissionHub> hub,
    ILogger logger, CancellationToken stopping)
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(1), stopping);
        await UpdateStatusAsync(submission, "processing", hub, logger, stopping);
        await Task.Delay(TimeSpan.FromSeconds(2), stopping);
        var finalStatus = submission.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "graded"
            : "failed";
        await UpdateStatusAsync(submission, finalStatus, hub, logger, stopping);
    }
    catch (OperationCanceledException) when (stopping.IsCancellationRequested)
    {
    }
}

async Task UpdateStatusAsync(SubmissionDto submission, string status,
    IHubContext<SubmissionHub> hub, ILogger logger, CancellationToken stopping)
{
    var updated = submission with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
    submissions[updated.Id] = updated;

    var message = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    try
    {
        await hub.Clients.Group(SubmissionHub.GroupFor(updated.Id))
            .SendAsync("SubmissionStatusChanged", message, stopping);
        logger.LogInformation("[Push] {SubmissionId} -> {Status}", updated.Id, status);
    }
    catch (Exception error) when (!stopping.IsCancellationRequested)
    {
        logger.LogWarning(error, "Khong gui duoc thong bao cho {SubmissionId}", updated.Id);
    }
}

sealed class SubmissionHub(ILogger<SubmissionHub> logger) : Hub
{
    public Task WatchSubmission(Guid submissionId)
    {
        logger.LogInformation("[Hub] {ConnectionId} theo doi {SubmissionId}", Context.ConnectionId, submissionId);
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(submissionId));
    }

    internal static string GroupFor(Guid id) => $"submission:{id}";
}

record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);

record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record ApiError(string Code, string Message);
