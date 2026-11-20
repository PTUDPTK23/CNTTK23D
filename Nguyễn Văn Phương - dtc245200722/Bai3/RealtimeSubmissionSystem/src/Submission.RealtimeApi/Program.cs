// EduSubmit realtime – Submission.RealtimeApi (máy chủ)
// Chạy: dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000

using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();            // trước builder.Build()
var app = builder.Build();

// Dữ liệu mẫu trong bộ nhớ
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Nhan thong bao trang thai thoi gian thuc", true),
    new(2, "Bai tap REST API", "Thiet ke endpoint cho he thong nop bai", true),
    new(3, "Bai tap MEP", "Bai tap da het han nop", false)
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();   // tác vụ nền + HTTP cùng sửa

// GET / : kiểm tra máy chủ đang chạy
app.MapGet("/", () => Results.Ok(new {
    service = "Submission.RealtimeApi",
    status = "running"
}));

// GET /api/assignments : danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// POST /api/assignments/{assignmentId:int}/submissions : nộp bài
// Kiểm tra theo thứ tự: tồn tại (404) → còn mở (400) → hợp lệ (400) → tạo (201)
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) => {
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: now,
        UpdatedAt: now);
    lock (submissionsLock) { submissions.Add(submission); }

    // Khởi động chấm bài ở nền, KHÔNG chờ: POST trả 201 ngay
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// GET /api/submissions/{submissionId:guid} : đọc lại lượt nộp
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    lock (submissionsLock) {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

// Hub thời gian thực: máy khách vào nhóm của lượt nộp để nhận trạng thái
app.MapHub<SubmissionHub>("/hubs/submissions");   // trước app.Run()

app.Run();

// Giả lập chấm bài: received → (1 giây) processing → (2 giây) graded
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

// Cập nhật dữ liệu trước, rồi mới đẩy thông báo tới đúng nhóm
static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate) {                                  // ① cập nhật dữ liệu
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0) {
            updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            list[i] = updated;
        }
    }
    if (updated is null) return;

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status,
                                          updated.UpdatedAt);   // ② dữ liệu tối thiểu
    var group = SubmissionGroups.ForSubmission(id);
    await hub.Clients.Group(group)
             .SendAsync("SubmissionStatusChanged", msg);        // ③ đẩy đúng nhóm
    Console.WriteLine($"[Push] {group} -> {status}");
}

// Hub mỏng: chỉ đưa kết nối vào nhóm submission:{id}
sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        var group = SubmissionGroups.ForSubmission(submissionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {group}");
    }
}

// Một chỗ duy nhất sinh tên nhóm → hai phía không lệch tên
static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// DTO: hình dạng dữ liệu đi qua mạng (khai báo sau app.Run())
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
