using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Bước 3: Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu mẫu bài tập
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Lam quen voi SignalR Hub va Client.", true),
    new(2, "Bai tap Client", "Viet console client goi Submission.RealtimeApi.", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object(); // Tác vụ nền + HTTP cùng sửa

// 1. Kiểm tra trạng thái máy chủ
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// 2. Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 3. Lấy thông tin bài tập theo ID
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// 4. Tạo lượt nộp bài mới + khởi động chấm bài ngầm (Bước 4.1)
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId,
    CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop."));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));
    }

    var studentCode = request.StudentCode.Trim();
    lock (submissionsLock)
    {
        var duplicated = submissions.Any(item =>
            item.AssignmentId == assignmentId &&
            item.StudentCode.Equals(studentCode, StringComparison.OrdinalIgnoreCase));
        if (duplicated)
        {
            return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));
        }

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(
            Guid.NewGuid(),
            assignmentId,
            studentCode,
            request.RepositoryUrl.Trim(),
            "received",
            now,
            now);
        submissions.Add(submission);

        // Khởi động chấm bài ở nền, KHÔNG chờ
        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    }
});

// 5. Đọc lại chi tiết lượt nộp theo ID
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

// Bước 3: Ánh xạ Hub SignalR
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// Bước 4.2: Hàm giả lập chấm bài và đẩy thông báo qua SignalR
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status, List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)
    {
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0)
        {
            updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            list[i] = updated;
        }
    }
    if (updated is null) return;

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

// Bước 3: Hub mỏng nhận kết nối và đưa vào nhóm theo lượt nộp
sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {SubmissionGroups.ForSubmission(submissionId)}");
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

// Các định nghĩa dữ liệu (DTO)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record ApiError(string Code, string Message);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
