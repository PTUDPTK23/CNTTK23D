using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tao API dau tien", false),
    new Assignment(2, "Bai tap 2", "Tao client goi API", true)
};

var submissions = new List<SubmissionDto>();

// Khóa dữ liệu dùng chung giữa HTTP và tác vụ nền
var submissionsLock = new object();

// GET: kiểm tra API
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// GET: danh sách bài tập
app.MapGet("/api/assignments", () =>
    Results.Ok(assignments));

// POST: tiếp nhận bài nộp (Bước 4.1: nhận IHubContext, chấm bài ở nền)
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
(int assignmentId,
 CreateSubmissionRequest request,
 IHubContext<SubmissionHub> hubContext) =>
{
    // 1. Kiểm tra bài tập tồn tại
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

    if (assignment is null)
    {
        return Results.NotFound(
            new ApiError("ASSIGNMENT_NOT_FOUND",
                "Không tìm thấy bài tập."));
    }

    // 2. Kiểm tra bài tập đã đóng
    if (assignment.IsClosed)
    {
        return Results.BadRequest(
            new ApiError("ASSIGNMENT_CLOSED",
                "Bài tập đã đóng."));
    }

    // 3. Kiểm tra dữ liệu hợp lệ
    if (string.IsNullOrWhiteSpace(request.StudentCode)
        || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(
            new ApiError("INVALID_SUBMISSION",
                "Dữ liệu bài nộp không hợp lệ."));
    }

    // 4. Tạo bài nộp ở trạng thái "received"
    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode,
        request.RepositoryUrl,
        "received",
        now,
        now
    );

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // 5. Khởi động chấm bài ở nền, KHÔNG chờ
    _ = SimulateGradingAsync(submission.Id, submissions,
        submissionsLock, hubContext);

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission);
});

// GET: lấy bài nộp theo ID
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(
            i => i.Id == submissionId);

        return submission is null
            ? Results.NotFound(
                new ApiError("SUBMISSION_NOT_FOUND",
                    "Không tìm thấy bài nộp."))
            : Results.Ok(submission);
    }
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

// Bước 4.2: giả lập chấm bài, đổi trạng thái received → processing → graded
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;

    // ① cập nhật dữ liệu trước
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

    if (updated is null) return;

    // ② dữ liệu tối thiểu: chỉ 3 trường
    var msg = new SubmissionStatusMessage(
        updated.Id, updated.Status, updated.UpdatedAt);

    // ③ đẩy đúng nhóm của lượt nộp
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);

    Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");
}

// Hub mỏng: chỉ đưa kết nối vào nhóm của lượt nộp
sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        var group = SubmissionGroups.ForSubmission(submissionId);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {group}");

        return Groups.AddToGroupAsync(Context.ConnectionId, group);
    }
}

// Một chỗ duy nhất sinh tên nhóm
static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

record Assignment(
    int Id,
    string Title,
    string Description,
    bool IsClosed
);

// assignmentId lấy từ đường dẫn, body chỉ có 2 trường
record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl
);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt
);

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt
);

record ApiError(string Code, string Message);