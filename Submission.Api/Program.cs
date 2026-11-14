var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu bài tập
var assignments = new[] {
    new Assignment(1, "Bai tap 1", "Tao API dau tien"),
    new Assignment(2, "Bai tap 2", "Goi API tu client")
};

// GET / : Kiểm tra trạng thái API
app.MapGet("/", () => Results.Ok(new { 
    service = "Submission.Api", 
    status = "running" 
}));

// GET /api/assignments : Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// POST /api/submissions : Xử lý nộp bài
app.MapPost("/api/submissions", (CreateSubmissionRequest request) => {
    // 1. Kiểm tra dữ liệu không hợp lệ
    if (request.AssignmentId <= 0 || string.IsNullOrWhiteSpace(request.StudentCode)) {
        return Results.BadRequest(new { message = "Du lieu khong hop le" });
    }
    
    // 2. Tạo kết quả nộp bài
    var result = new SubmissionResult(
        Guid.NewGuid(), 
        request.AssignmentId, 
        request.StudentCode, 
        "received", 
        DateTimeOffset.UtcNow
    );
    
    // 3. Trả về mã 201 Created cùng dữ liệu
    return Results.Created($"/api/submissions/{result.Id}", result);
});

app.Run();

// Khai báo các cấu trúc dữ liệu
record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);