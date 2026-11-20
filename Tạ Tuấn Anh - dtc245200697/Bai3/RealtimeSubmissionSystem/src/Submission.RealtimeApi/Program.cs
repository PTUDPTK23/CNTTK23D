using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu mẫu trong bộ nhớ
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Theo doi trang thai luot nop thoi gian thuc bang SignalR.", true),
    new(2, "Bai tap REST API", "Thiet ke endpoint RESTful cho he thong nop bai.", true),
    new(3, "Bai tap da dong", "Bai tap nay da khoa de kiem thu loi 400 ASSIGNMENT_CLOSED.", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object(); // tác vụ nền + HTTP cùng đọc/ghi

// 1. GET / : kiểm tra máy chủ đang chạy
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// 2. GET /api/assignments : danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{assignmentId:int} : chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// 3. POST /api/assignments/{assignmentId:int}/submissions : tạo lượt nộp + khởi động chấm nền
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) =>
    {
        var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong nop."));

        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

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

        // Khởi động tác vụ chấm nền, KHÔNG await để trả 201 Created ngay lập tức
        _ = SimulateGradingAsync(submission.Id, submission.RepositoryUrl, submissions, submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

// 4. GET /api/submissions/{submissionId:guid} : đọc lại lượt nộp (REST là nguồn sự thật)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

// Ánh xạ SignalR Hub
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
    // Bài tập 2: RepositoryUrl không bắt đầu bằng https:// -> trạng thái cuối là failed
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
    lock (gate) // ① Cập nhật dữ liệu gốc trước
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

    if (updated is null) return;

    // ② Gói dữ liệu tối thiểu
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    var groupName = SubmissionGroups.ForSubmission(id);

    Console.WriteLine($"[Push] {groupName} -> {updated.Status}");

    // ③ Đẩy thông báo tới đúng nhóm đang theo dõi lượt nộp
    await hub.Clients.Group(groupName)
        .SendAsync("SubmissionStatusChanged", msg);
}

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        var groupName = SubmissionGroups.ForSubmission(submissionId);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {groupName}");
        return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// Hợp đồng dữ liệu (DTO)
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
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
