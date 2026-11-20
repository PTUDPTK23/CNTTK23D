using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

var baseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "DTC245200318";
var repositoryUrl = args.Length > 2 ? args[2] : "https://github.com/anhphann1412-max/CNTTK23D";
var eventName = args.Contains("--event-name-mismatch")
    ? "SubmissionStatusChange"
    : "SubmissionStatusChanged";
var watchDelay = args.FirstOrDefault(a => a.StartsWith("--watch-delay=", StringComparison.Ordinal));
var delaySeconds = watchDelay is null ? 0 : int.Parse(watchDelay[14..]);
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

var finalStatus = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
string? watchedSubmissionId = null;
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{baseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

connection.On<SubmissionStatusMessage>(eventName, message =>
{
    Console.WriteLine($"Thông báo SignalR: {message.SubmissionId} -> {message.Status} lúc {message.UpdatedAt:HH:mm:ss}");
    if (message.Status is "graded" or "failed")
    {
        finalStatus.TrySetResult(message.Status);
    }
});

connection.Reconnected += async _ =>
{
    if (watchedSubmissionId is not null)
    {
        await connection.InvokeAsync("WatchSubmission", watchedSubmissionId);
    }
};

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments")
    ?? throw new InvalidOperationException("API không trả danh sách bài tập.");
var selected = assignments.FirstOrDefault()
    ?? throw new InvalidOperationException("API chưa có bài tập để nộp.");
Console.WriteLine($"Chọn bài tập: {selected.Id} - {selected.Title}");

await connection.StartAsync();
Console.WriteLine("Đã kết nối SignalR.");
using var response = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, repositoryUrl));
if (!response.IsSuccessStatusCode)
{
    var body = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"REST POST lỗi {(int)response.StatusCode}: {body}");
    return;
}

var created = await response.Content.ReadFromJsonAsync<SubmissionDto>()
    ?? throw new InvalidOperationException("REST POST không trả lượt nộp.");
Console.WriteLine($"Đã tạo lượt nộp: {created.Id}");
Console.WriteLine($"Trạng thái REST ban đầu: {created.Status}");
watchedSubmissionId = created.Id.ToString();
if (delaySeconds > 0)
{
    Console.WriteLine($"Thí nghiệm: chờ {delaySeconds} giây trước khi vào nhóm.");
    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
}
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
Console.WriteLine("Đang theo dõi trạng thái bằng SignalR...");

await Task.WhenAny(finalStatus.Task, Task.Delay(TimeSpan.FromSeconds(5)));
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
if (latest is null)
{
    Console.WriteLine("REST GET cuối không tìm thấy lượt nộp.");
    return;
}

Console.WriteLine($"Trạng thái đọc lại bằng REST: {latest.Status}");
if (latest.Status == "failed")
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("CẢNH BÁO: lượt nộp bị failed; kiểm tra RepositoryUrl.");
    Console.ResetColor();
}

sealed record AssignmentDto(int Id, string Title);
sealed record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
sealed record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
sealed record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
