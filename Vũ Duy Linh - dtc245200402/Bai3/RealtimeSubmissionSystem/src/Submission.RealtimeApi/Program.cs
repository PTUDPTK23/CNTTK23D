
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. ĐĂNG KÝ DỊCH VỤ
// =====================================================

builder.Services.AddSignalR();

var app = builder.Build();


// =====================================================
// 2. DỮ LIỆU MẪU
// =====================================================

// Danh sách bài tập
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", true),
    new(2, "Bai tap da dong", false)
};

// Danh sách lượt nộp bài
var submissions = new List<SubmissionDto>();

// Khóa đồng bộ khi HTTP và tác vụ nền cùng đọc/ghi
var submissionsLock = new object();


// =====================================================
// 3. API KIỂM TRA SERVER
// GET /
// =====================================================

app.MapGet("/", () => Results.Ok(
    new { service = "EduSubmit", status = "running" }
));


// =====================================================
// 4. GET DANH SÁCH BÀI TẬP
// GET /api/assignments
// =====================================================

app.MapGet("/api/assignments", () =>
{
    return Results.Ok(assignments);
});


// =====================================================
// 5. POST TẠO LƯỢT NỘP BÀI
// POST /api/assignments/{assignmentId}/submissions
// =====================================================

app.MapPost(
    "/api/assignments/{assignmentId:int}/submissions",
    (
        int assignmentId,
        CreateSubmissionRequest request,
        IHubContext<SubmissionHub> hubContext
    ) =>
{
    // Kiểm tra bài tập có tồn tại không
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId
    );

    // 404: Không tìm thấy bài tập
    if (assignment is null)
    {
        return Results.NotFound(
            new ApiError(
                "ASSIGNMENT_NOT_FOUND",
                "Khong tim thay bai tap."
            )
        );
    }

    // 400 CLOSED: Bài tập đã đóng
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(
            new ApiError(
                "ASSIGNMENT_CLOSED",
                "Bai tap da dong."
            )
        );
    }

    // 400 INVALID: Dữ liệu không hợp lệ
    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(
            new ApiError(
                "INVALID_SUBMISSION",
                "StudentCode va RepositoryUrl khong duoc de trong."
            )
        );
    }

    // Tạo lượt nộp mới
    var now = DateTimeOffset.UtcNow;

    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode.Trim(),
        RepositoryUrl: request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: now,
        UpdatedAt: now
    );

    // Thêm lượt nộp vào danh sách có khóa
    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khởi động chấm bài ở nền, không chờ
    _ = SimulateGradingAsync(
        submission.Id,
        submissions,
        submissionsLock,
        hubContext
    );

    // Trả HTTP 201 Created ngay lập tức
    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission
    );
});


// =====================================================
// 6. GET THÔNG TIN LƯỢT NỘP
// GET /api/submissions/{submissionId}
// =====================================================

app.MapGet(
    "/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(
            i => i.Id == submissionId
        );

        return submission is null
            ? Results.NotFound(
                new ApiError(
                    "SUBMISSION_NOT_FOUND",
                    "Khong tim thay luot nop bai."
                )
            )
            : Results.Ok(submission);
    }
});


// =====================================================
// 7. ÁNH XẠ SIGNALR HUB
// =====================================================

app.MapHub<SubmissionHub>("/hubs/submissions");


// =====================================================
// 8. CHẠY SERVER
// =====================================================

app.Run();


// =====================================================
// 9. MÔ PHỎNG CHẤM BÀI NỀN
// =====================================================

static async Task SimulateGradingAsync(
    Guid id,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    // Chờ 1 giây
    await Task.Delay(TimeSpan.FromSeconds(1));

    // Chuyển sang processing
    await UpdateStatusAsync(
        id,
        "processing",
        list,
        gate,
        hub
    );

    // Chờ thêm 2 giây
    await Task.Delay(TimeSpan.FromSeconds(2));

    // Chuyển sang graded
    await UpdateStatusAsync(
        id,
        "graded",
        list,
        gate,
        hub
    );
}


// =====================================================
// 10. CẬP NHẬT TRẠNG THÁI VÀ GỬI THÔNG BÁO
// =====================================================

static async Task UpdateStatusAsync(
    Guid id,
    string status,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;

    // ① Cập nhật dữ liệu trước
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

    if (updated is null)
        return;

    // ② Tạo thông điệp trạng thái
    var msg = new SubmissionStatusMessage(
        updated.Id,
        updated.Status,
        updated.UpdatedAt
    );

    // Ghi nhật ký trên máy chủ
    Console.WriteLine(
        $"[Push] {updated.Id} -> {updated.Status}"
    );

    // ③ Gửi thông báo đến đúng nhóm
    await hub.Clients
        .Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}


// =====================================================
// 11. DATA TRANSFER OBJECTS
// =====================================================

// Thông tin bài tập
record AssignmentDto(
    int Id,
    string Title,
    bool IsOpen
);

// Dữ liệu nhận từ Client
record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl
);

// Thông tin lượt nộp
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt
);

// Thông điệp trạng thái lượt nộp
record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt
);

// Cấu trúc thông báo lỗi
record ApiError(
    string Code,
    string Message
);


// =====================================================
// 12. SIGNALR HUB
// =====================================================

sealed class SubmissionHub : Hub
{
    // Máy khách đăng ký theo dõi một lượt nộp
    public Task WatchSubmission(string submissionId)
    {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId)
        );
    }
}


// =====================================================
// 13. QUẢN LÝ TÊN NHÓM
// =====================================================

static class SubmissionGroups
{
    // Nhận ID dạng Guid
    public static string ForSubmission(Guid id)
        => ForSubmission(id.ToString());

    // Nhận ID dạng string
    public static string ForSubmission(string id)
        => $"submission:{id}";
}
