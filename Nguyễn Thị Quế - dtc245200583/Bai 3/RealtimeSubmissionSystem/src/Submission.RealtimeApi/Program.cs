using Microsoft.AspNetCore.SignalR;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

// Du lieu mau: bai 1 va 2 dang mo, bai 3 da dong.
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Xay dung API theo chuan RESTful.", true),
    new(2, "Bai tap Client", "Su dung HttpClient de goi API.", true),
    new(3, "Bai tap nang cao", "Bai tap da het han nop.", false)
};

// Danh sach luot nop va khoa dong bo du lieu.
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// 1. Danh sach bai tap
app.MapGet("/api/assignments", () =>
    Results.Ok(assignments));

// 2. Chi tiet bai tap
app.MapGet("/api/assignments/{id:int}", (int id) =>
{
    var assignment = assignments.FirstOrDefault(x => x.Id == id);

    return assignment is null
        ? Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            $"Khong tim thay bai tap co id {id}."))
        : Results.Ok(assignment);
});

// 3. Tao luot nop bai
app.MapPost("/api/assignments/{id:int}/submissions",
    (int id,
     CreateSubmissionRequest? request,
     IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(x => x.Id == id);

    // Kiem tra bai tap ton tai
    if (assignment is null)
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            $"Khong tim thay bai tap co id {id}."));

    // Kiem tra bai tap con mo
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError(
            "ASSIGNMENT_CLOSED",
            "Bai tap da dong, khong the nop bai."));

    // Kiem tra du lieu dau vao
    if (request is null ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION",
            "Thieu ma sinh vien hoac kho ma nguon."));

    if (!Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out var repoUri) ||
        (repoUri.Scheme != Uri.UriSchemeHttp &&
         repoUri.Scheme != Uri.UriSchemeHttps))
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION",
            "repositoryUrl phai la URL http hoac https hop le."));

    var now = DateTimeOffset.UtcNow;

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignment.Id,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        now,
        now
    );

    // Khoa khi them du lieu
    lock (submissionsLock)
{
    submissions.Add(submission);
}

// Khoi dong tac vu cham bai nen, khong cho POST phai doi
_ = SimulateGradingAsync(
    submission.Id,
    submissions,
    submissionsLock,
    hubContext);

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission);
});

// 4. Doc lai mot luot nop
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(
            x => x.Id == submissionId);

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
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(30));

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

    // ① Cap nhat du lieu truoc
    lock (gate)
    {
        var i = list.FindIndex(s => s.Id == id);

        if (i >= 0)
        {
            updated = list[i] with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            list[i] = updated;
        }
    }

    if (updated is null)
        return;

    // ② Tao thong diep toi thieu
    var msg = new SubmissionStatusMessage(
        updated.Id,
        updated.Status,
        updated.UpdatedAt);

    // ③ Gui thong bao den dung nhom
    await hub.Clients
        .Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);


record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen);

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

record ApiError(
    string Code,
    string Message);

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
    public static string ForSubmission(Guid id)
        => ForSubmission(id.ToString());

    public static string ForSubmission(string id)
        => $"submission:{id}";
}

