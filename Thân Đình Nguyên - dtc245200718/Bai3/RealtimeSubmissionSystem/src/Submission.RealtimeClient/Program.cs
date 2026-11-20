using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

var studentCode = args.Length > 1
    ? args[1]
    : "DTC245200718";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource();

connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged",
    message =>
    {
        Console.WriteLine(
            $"Thong bao SignalR: {message.SubmissionId} -> {message.Status}");

        if (message.Status == "graded")
            gradedNotification.TrySetResult();
    });

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// 1. Lấy danh sách bài tập bằng REST
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>(
    "/api/assignments");

Console.WriteLine("Danh sach bai tap:");
foreach (var assignment in assignments ?? [])
{
    Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
}

var selected = assignments?.FirstOrDefault(a => a.IsOpen);
if (selected is null)
{
    Console.WriteLine("Khong co bai tap dang mo.");
    return;
}

// 2. Tạo lượt nộp bằng REST
var request = new CreateSubmissionRequest(
    studentCode,
    "https://github.com/ThanDinhNguyen/CNTTK23D");

using var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    request);

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine(
        $"Nop bai that bai: {(int)createResponse.StatusCode} " +
        $"{error?.Code} - {error?.Message}");
    return;
}

var created = await createResponse.Content
    .ReadFromJsonAsync<SubmissionDto>();

if (created is null)
{
    Console.WriteLine("API khong tra ve du lieu luot nop.");
    return;
}

Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// 3. Theo dõi đúng lượt nộp vừa tạo
await connection.InvokeAsync(
    "WatchSubmission",
    created.Id.ToString());

Console.WriteLine("Da dang ky theo doi luot nop.");

// 4. Chờ thông báo graded, tối đa 10 giây
var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task)
    Console.WriteLine("Qua thoi gian cho thong bao graded.");

// 5. Đọc lại trạng thái cuối bằng REST
var latest = await http.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{created.Id}");

Console.WriteLine(
    $"Trang thai doc lai bang REST: {latest?.Status}");

record AssignmentDto(
    int Id, string Title, string Description, bool IsOpen);

record CreateSubmissionRequest(
    string StudentCode, string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);

record ApiError(string Code, string Message);