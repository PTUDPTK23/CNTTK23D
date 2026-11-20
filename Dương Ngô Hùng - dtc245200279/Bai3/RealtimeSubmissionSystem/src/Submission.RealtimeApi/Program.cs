using Microsoft.AspNetCore.SignalR;

// ① Khởi tạo
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();            // trước builder.Build()
var app = builder.Build();

// ② Dữ liệu mẫu trong bộ nhớ (giữ nguyên Bài 2): tắt API là mất
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint cho he thong nop bai", true),
    new(2, "Bai tap Client", "Viet console client goi API bang HttpClient", true),
    new(3, "Bai tap da dong", "Bai tap da het han nop", false)
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();   // tác vụ nền + HTTP cùng sửa

// ③ Các endpoint REST (giữ nguyên Bài 2)

// GET / : kiểm tra máy chủ đang chạy
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// GET /api/assignments : danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{assignmentId:int} : chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// POST /api/assignments/{assignmentId:int}/submissions : nộp bài
// Kiểm tra theo thứ tự: tồn tại (404) → còn mở (400 CLOSED) → body hợp lệ (400 INVALID), rồi mới tạo (201).
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION",
            "Thieu ma sinh vien hoac kho ma nguon."));

    // Máy chủ tự sinh Id, Status, ReceivedAt, UpdatedAt: máy khách không được quyết định
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

    // Khởi động chấm bài ở nền, KHÔNG chờ: POST trả 201 ngay, chấm tiếp tục phía sau
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// GET /api/submissions/{submissionId:guid} : đọc lại lượt nộp (nguồn sự thật)
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

// Kênh thời gian thực: máy khách vào nhóm submission:{id} để nhận thông báo trạng thái
app.MapHub<SubmissionHub>("/hubs/submissions");   // trước app.Run()

// ④ Chạy
app.Run();

// Mô phỏng chấm bài ở nền: received → (1 giây) processing → (2 giây) graded
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

// Lưu trước, đẩy sau: ai đọc REST ngay khi nhận tin đều thấy đúng trạng thái
static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)                                     // ① cập nhật dữ liệu
    {
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0)
        {
            updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            list[i] = updated;
        }
    }
    if (updated is null) return;

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status,
                                          updated.UpdatedAt);           // ② dữ liệu tối thiểu
    var group = SubmissionGroups.ForSubmission(id);
    await hub.Clients.Group(group).SendAsync("SubmissionStatusChanged", msg);   // ③ đẩy đúng nhóm
    Console.WriteLine($"[Push] {group} -> {status}");
}

// Hub mỏng: không tạo, không chấm bài; chỉ đưa kết nối vào nhóm của lượt nộp
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

// Một chỗ duy nhất sinh tên nhóm → hub và tác vụ nền không lệch tên
static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// ⑤ Khai báo kiểu: DTO là hợp đồng dữ liệu đi qua mạng
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);

// Đầu vào: chỉ chứa những gì máy khách được phép quyết định
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

// Đầu ra: thêm các trường máy chủ tự sinh; UpdatedAt = thời điểm đổi trạng thái gần nhất
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record ApiError(string Code, string Message);

// Thông điệp đẩy qua SignalR: chỉ 3 trường tối thiểu, không gửi cả SubmissionDto
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
