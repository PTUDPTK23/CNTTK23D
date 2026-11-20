using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();            // dang ky SignalR, truoc builder.Build()
var app = builder.Build();

// Du lieu mau trong bo nho
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Nhan thong bao trang thai luot nop theo thoi gian thuc", true),
    new(2, "Bai tap REST API", "Thiet ke endpoint cho he thong nop bai", true),
    new(3, "Bai tap da dong", "Bai tap da het han, khong nhan them luot nop", false)
};

// Cac luot nop da nhan
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();   // tac vu nen + HTTP cung sua danh sach

// GET / : kiem tra may chu dang chay
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// GET /api/assignments : danh sach bai tap
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{assignmentId:int} : chi tiet mot bai tap
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// POST /api/assignments/{assignmentId:int}/submissions : nop bai (giu nguyen Bai 2)
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) =>   // "loa phat" cua hub dung NGOAI hub
{
    // Cong 1: bai tap co ton tai khong?
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    // Cong 2: bai tap con mo khong?
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan them luot nop."));

    // Cong 3: du lieu gui len co hop le khong?
    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac duong dan repository."));

    // May chu tu dien Id, AssignmentId, Status, ReceivedAt, UpdatedAt
    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        now,
        now);

    lock (submissionsLock) { submissions.Add(submission); }

    // Khoi dong cham bai o nen, KHONG cho: POST tra 201 ngay
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// GET /api/submissions/{submissionId:guid} : doc lai mot luot nop (nguon su that)
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

app.MapHub<SubmissionHub>("/hubs/submissions");   // truoc app.Run()

app.Run();

// Gia lap cham bai: received -> (1 giay) processing -> (2 giay) graded
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

// Luu truoc, day sau: ai doc REST ngay khi nhan tin deu thay dung
static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)                                            // (1) cap nhat du lieu
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
                                          updated.UpdatedAt);   // (2) du lieu toi thieu
    var group = SubmissionGroups.ForSubmission(id);
    Console.WriteLine($"[Push] {group} -> {status}");
    await hub.Clients.Group(group)
             .SendAsync("SubmissionStatusChanged", msg);       // (3) day dung nhom
}

// Hub mong: chi dua ket noi vao nhom cua luot nop, khong tao/cham bai
sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        var group = SubmissionGroups.ForSubmission(submissionId);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {group}");
        return Groups.AddToGroupAsync(Context.ConnectionId, group);
    }
}

// Mot cho duy nhat sinh ten nhom -> hai phia khong lech ten
static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// DTO: hinh dang du lieu di qua mang (dat sau app.Run())
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);   // UpdatedAt: thoi diem doi trang thai gan nhat
record ApiError(string Code, string Message);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt); // tin day qua SignalR
