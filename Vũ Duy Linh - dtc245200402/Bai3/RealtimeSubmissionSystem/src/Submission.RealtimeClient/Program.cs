
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

// =====================================================
// 1. KHAI BÁO URL SERVER VÀ MÃ SINH VIÊN
// =====================================================

var apiBaseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

var studentCode = args.Length > 1
    ? args[1]
    : "SV001";


// =====================================================
// 2. TẠO KẾT NỐI REST
// =====================================================

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};


// =====================================================
// 3. TẠO KẾT NỐI SIGNALR
// =====================================================

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();


// =====================================================
// 4. TẠO TASKCOMPLETIONSOURCE
// =====================================================

// Chờ cho đến khi nhận được trạng thái graded
var gradedNotification = new TaskCompletionSource();


// =====================================================
// 5. ĐĂNG KÝ NHẬN THÔNG ĐIỆP SIGNALR
// =====================================================

connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged",
    message =>
    {
        Console.WriteLine(
            $"Thong bao SignalR: {message.SubmissionId} -> " +
            $"{message.Status} luc " +
            $"{message.UpdatedAt.ToLocalTime():HH:mm:ss}"
        );

        // Khi nhận graded thì hoàn thành tác vụ chờ
        if (message.Status == "graded")
        {
            gradedNotification.TrySetResult();
        }
    }
);


// =====================================================
// 6. KHỞI ĐỘNG KẾT NỐI SIGNALR
// =====================================================

await connection.StartAsync();

Console.WriteLine("Da ket noi SignalR.");


// =====================================================
// 7. GET DANH SÁCH BÀI TẬP
// =====================================================

var assignments =
    await http.GetFromJsonAsync<List<AssignmentDto>>(
        "/api/assignments"
    );

var selected = assignments?.FirstOrDefault();

if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao.");
    return;
}

Console.WriteLine(
    $"Chon bai tap: {selected.Id} - {selected.Title}"
);


// =====================================================
// 8. POST TẠO LƯỢT NỘP BÀI
// =====================================================

using var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(
        studentCode,
        $"https://example.com/{studentCode}/signalr"
    )
);


// =====================================================
// 9. KIỂM TRA HTTP 201 CREATED
// =====================================================

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var apiError =
        await createResponse.Content.ReadFromJsonAsync<ApiError>();

    Console.WriteLine(
        $"Loi tao luot nop: {apiError?.Code} - {apiError?.Message}"
    );

    return;
}


// =====================================================
// 10. ĐỌC THÔNG TIN LƯỢT NỘP VỪA TẠO
// =====================================================

var created =
    await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();

if (created is null)
{
    Console.WriteLine("Khong doc duoc thong tin luot nop.");
    return;
}

Console.WriteLine($"Da tao luot nop: {created.Id}");

Console.WriteLine(
    $"Trang thai REST ban dau: {created.Status}"
);


// =====================================================
// 11. GỌI WATCHSUBMISSION ĐỂ THAM GIA NHÓM
// =====================================================

await connection.InvokeAsync(
    "WatchSubmission",
    created.Id.ToString()
);


// =====================================================
// 12. CHỜ THÔNG BÁO GRADED HOẶC HẾT 10 GIÂY
// =====================================================

Console.WriteLine(
    "Dang cho thong bao trang thai tu may chu..."
);

var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10))
);

if (completed != gradedNotification.Task)
{
    Console.WriteLine(
        "Qua thoi gian cho thong bao graded."
    );
}


// =====================================================
// 13. GET LẠI TRẠNG THÁI CUỐI TỪ REST
// =====================================================

var latest = await http.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{created.Id}"
);

Console.WriteLine(
    $"Trang thai doc lai bang REST: {latest?.Status}"
);


// =====================================================
// 14. DATA TRANSFER OBJECTS
// =====================================================

// Thông tin bài tập
record AssignmentDto(
    int Id,
    string Title,
    bool IsOpen
);

// Dữ liệu gửi lên khi tạo lượt nộp
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

// Thông điệp trạng thái nhận từ SignalR
record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt
);

// Cấu trúc thông báo lỗi từ API
record ApiError(
    string Code,
    string Message
);
