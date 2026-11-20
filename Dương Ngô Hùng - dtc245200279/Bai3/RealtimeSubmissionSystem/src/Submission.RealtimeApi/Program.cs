// ① Khởi tạo
var builder = WebApplication.CreateBuilder(args);
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
    (int assignmentId, CreateSubmissionRequest request) =>
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

// ④ Chạy
app.Run();

// ⑤ Khai báo kiểu: DTO là hợp đồng dữ liệu đi qua mạng
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);

// Đầu vào: chỉ chứa những gì máy khách được phép quyết định
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

// Đầu ra: thêm các trường máy chủ tự sinh; UpdatedAt = thời điểm đổi trạng thái gần nhất
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record ApiError(string Code, string Message);
