using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();            // Bước 3: trước builder.Build()
var app = builder.Build();

// Dữ liệu trong bộ nhớ (tắt API là mất)
var assignments = new List<AssignmentDto> {
    new(1, "Bai tap SignalR", "Nhan thong bao trang thai thoi gian thuc", true),
    new(2, "Bai tap REST API", "Thiet ke endpoint cho he thong nop bai", true)
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();   // Bước 2: tác vụ nền + HTTP cùng sửa

app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) => {
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Bước 2 + 4.1: POST giữ nguyên Bài 2, thêm UpdatedAt, lock và IHubContext
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) => {

    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION",
            "Thieu ma sinh vien hoac kho ma nguon."));

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(Guid.NewGuid(), assignmentId,
        request.StudentCode.Trim(), request.RepositoryUrl.Trim(),
        Status: "received", ReceivedAt: now, UpdatedAt: now);
    lock (submissionsLock) { submissions.Add(submission); }

    // khởi động chấm bài ở nền, KHÔNG chờ
    _ = SimulateGradingAsync(submission.Id, submissions,
                             submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    lock (submissionsLock) {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

app.MapHub<SubmissionHub>("/hubs/submissions");   // Bước 3: trước app.Run()

app.Run();

// Bước 4.2: giả lập chấm bài ở nền
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub) {
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub) {
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
    Console.WriteLine($"[Push] {group} -> {status}");
    await hub.Clients.Group(group)
             .SendAsync("SubmissionStatusChanged", msg);   // ③ đẩy đúng nhóm
}

// Bước 3: hub mỏng, chỉ đưa kết nối vào nhóm của lượt nộp
sealed class SubmissionHub : Hub {
    public Task WatchSubmission(string submissionId) {
        var group = SubmissionGroups.ForSubmission(submissionId);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {group}");
        return Groups.AddToGroupAsync(Context.ConnectionId, group);
    }
}

static class SubmissionGroups {
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);