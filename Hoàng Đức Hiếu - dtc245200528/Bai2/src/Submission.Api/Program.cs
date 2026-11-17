var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu trong bộ nhớ (tắt API là mất)
var assignments = new List<AssignmentDto> {
    new(1, "Bai tap REST API", "Thiet ke endpoint cho he thong nop bai", true),
    new(2, "Bai tap Client", "Viet console client goi API", true)
};
var submissions = new List<SubmissionDto>();

// Bước 2: kiểm tra máy chủ đang chạy
app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

// Bước 3: danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Bước 4: chi tiết một bài tập, 200 nếu có, 404 kèm ApiError nếu không
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) => {
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Bước 5: nộp bài - kiểm tra theo thứ tự: tồn tại -> còn mở -> hợp lệ, rồi trả 201
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) => {
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION",
            "Thieu ma sinh vien hoac kho ma nguon."));

    // Id, AssignmentId, Status, ReceivedAt do máy chủ tự sinh
    var submission = new SubmissionDto(Guid.NewGuid(), assignmentId,
        request.StudentCode.Trim(), request.RepositoryUrl.Trim(),
        "received", DateTimeOffset.UtcNow);
    submissions.Add(submission);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// Bước 6: đọc lại lượt nộp theo header Location
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.Run();

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
