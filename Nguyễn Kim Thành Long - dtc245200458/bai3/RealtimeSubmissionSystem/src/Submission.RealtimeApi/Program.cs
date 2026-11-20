using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR vào DI (Trước builder.Build)
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu mẫu bài tập trong bộ nhớ
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Xay dung kenh thoi gian thuc voi ASP.NET Core SignalR", DateTimeOffset.UtcNow.AddDays(7)),
    new(2, "Bai tap RESTful API", "Thiet ke RESTful API voi ASP.NET Core Minimal API", DateTimeOffset.UtcNow.AddDays(14))
};

// Danh sách các lượt nộp bài và khoá đồng bộ thread-safe
var submissions = new List<SubmissionDto>();
var submissionsLock = new object(); // tác vụ nền + HTTP cùng sửa

// 1. GET / : kiểm tra trạng thái hoạt động của dịch vụ
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// 2. GET /api/assignments : danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 3. POST /api/assignments/{assignmentId:int}/submissions : tạo lượt nộp bài
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
{
    // Kiểm tra bài tập tồn tại (404)
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap voi Id = {assignmentId}."));
    }

    // Kiểm tra hạn nộp bài (400 CLOSED)
    if (assignment.DueDate < DateTimeOffset.UtcNow)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da het han nop."));
    }

    // Kiểm tra dữ liệu đầu vào (400 INVALID)
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "StudentCode va RepositoryUrl khong duoc de trong."));
    }

    // Tạo lượt nộp bài với trạng thái ban đầu "received"
    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode.Trim(),
        RepositoryUrl: request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow);

    // Ghi vào danh sách với lock để đồng bộ an toàn giữa các luồng
    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khởi động tác vụ chấm bài ở nền, KHÔNG chờ (Fire-and-forget)
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    // Trả về HTTP 201 Created kèm header Location và đối tượng submission
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 4. GET /api/submissions/{submissionId:guid} : đọc chi tiết lượt nộp (Nguồn sự thật)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", $"Khong tim thay luot nop voi Id = {submissionId}."))
            : Results.Ok(submission);
    }
});

// 5. Ánh xạ SignalR Hub vào tuyến /hubs/submissions
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// ==================== CÁC PHƯƠNG THỨC NỀN & SIGNALR HUB ====================

// Tác vụ nền giả lập quá trình chấm bài bất đồng bộ
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    // Sau 1 giây chuyển sang "processing"
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    // Sau thêm 2 giây (tổng 3 giây) chuyển sang "graded"
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

// Cập nhật trạng thái lượt nộp và phát thông báo qua SignalR
static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)
    {
        // ① Cập nhật dữ liệu vào danh sách (Lưu trước)
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

    // ② Tạo thông điệp dữ liệu tối thiểu
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);

    // In nhật ký tại máy chủ
    Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");

    // ③ Đẩy đúng nhóm máy khách đang theo dõi qua SignalR
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

// Hub quản lý kết nối và nhóm theo dõi bài nộp
sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        var groupName = SubmissionGroups.ForSubmission(submissionId);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {groupName}");
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}

// Lớp tiện ích sinh tên nhóm thống nhất, tránh lệch tên giữa các bên
static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// ==================== DTO & CONTRACT MODELS ====================

record AssignmentDto(int Id, string Title, string Description, DateTimeOffset DueDate);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record ApiError(string Code, string Message);
