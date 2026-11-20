using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var repositoryUrl = args.Length > 2 ? args[2] : $"https://example.com/{studentCode}/signalr";

// Kênh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var finalNotification = new TaskCompletionSource(); // "cái chuông" chờ trạng thái cuối

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    if (string.Equals(m.Status, "failed", StringComparison.OrdinalIgnoreCase))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId} cham LOI (failed) luc {m.UpdatedAt.ToLocalTime():HH:mm:ss}");
        Console.ResetColor();
        finalNotification.TrySetResult();
    }
    else
    {
        Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {m.UpdatedAt.ToLocalTime():HH:mm:ss}");
        if (string.Equals(m.Status, "graded", StringComparison.OrdinalIgnoreCase))
        {
            finalNotification.TrySetResult();
        }
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// ① REST: Lấy danh sách bài tập đang mở
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault(a => a.IsOpen);
if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao dang mo.");
    return;
}
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

// ② REST: Tạo lượt nộp mới
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, repositoryUrl));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi tao luot nop ({(int)createResponse.StatusCode}): {error?.Code} - {error?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
if (created is null)
{
    Console.WriteLine("Khong doc duoc du lieu luot nop.");
    return;
}

Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// ③ SignalR: Đăng ký vào nhóm submission:{id} để theo dõi
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// ④ Chờ thông báo graded/failed hoặc hết 10 giây
var completed = await Task.WhenAny(
    finalNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != finalNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// ⑤ REST: Đọc lại trạng thái cuối từ nguồn sự thật
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

// Hợp đồng dữ liệu (DTO) giống phía máy chủ
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
