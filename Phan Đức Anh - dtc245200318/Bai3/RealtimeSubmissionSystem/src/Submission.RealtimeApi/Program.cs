using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var assignments = new[] { new AssignmentDto(1, "Bai tap SignalR") };
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));
app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest? request) =>
    {
        if (assignments.All(a => a.Id != assignmentId))
        {
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Không tìm thấy bài tập."));
        }

        if (request is null || string.IsNullOrWhiteSpace(request.StudentCode)
            || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new ApiError("SUBMISSION_INVALID",
                "StudentCode và RepositoryUrl là bắt buộc."));
        }

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(Guid.NewGuid(), assignmentId,
            request.StudentCode.Trim(), request.RepositoryUrl.Trim(), "received", now, now);
        lock (submissionsLock)
        {
            submissions.Add(submission);
        }

        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock,
            app.Services.GetRequiredService<IHubContext<SubmissionHub>>(), app.Lifetime.ApplicationStopping);
        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    SubmissionDto? submission;
    lock (submissionsLock)
    {
        submission = submissions.FirstOrDefault(s => s.Id == submissionId);
    }

    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Không tìm thấy lượt nộp."))
        : Results.Ok(submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> submissions, object gate,
    IHubContext<SubmissionHub> hub, CancellationToken stoppingToken)
{
    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    await UpdateStatusAsync(id, "processing");
    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
    await UpdateStatusAsync(id, null);

    async Task UpdateStatusAsync(Guid submissionId, string? requestedStatus)
    {
        SubmissionDto? updated;
        lock (gate)
        {
            var index = submissions.FindIndex(s => s.Id == submissionId);
            if (index < 0)
            {
                return;
            }

            var current = submissions[index];
            var status = requestedStatus ?? (Uri.TryCreate(current.RepositoryUrl, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps ? "graded" : "failed");
            updated = current with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            submissions[index] = updated;
        }

        var message = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
        await hub.Clients.Group(SubmissionGroups.For(updated.Id))
            .SendAsync("SubmissionStatusChanged", message, stoppingToken);
        Console.WriteLine($"[Push] submission:{updated.Id} -> {updated.Status}");
    }
}

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        if (!Guid.TryParse(submissionId, out var id))
        {
            throw new HubException("SubmissionId phải là GUID hợp lệ.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SubmissionGroups.For(id));
        Console.WriteLine($"[Hub] {Context.ConnectionId} theo dõi submission:{id}");
    }
}

static class SubmissionGroups
{
    public static string For(Guid id) => $"submission:{id:D}";
}

sealed record AssignmentDto(int Id, string Title);
sealed record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
sealed record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
sealed record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
sealed record ApiError(string Code, string Message);
