using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var twoSubmissions = args.Length > 1 && args[1] == "--two";
var studentCode = args.Length > 1 && !twoSubmissions ? args[1] : "SV001";
var repositoryUrl = args.Length > 2 ? args[2] : $"https://example.com/{studentCode}/signalr";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var notifications = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt.ToLocalTime():HH:mm:ss}");
    if (message.Status == "failed")
    {
        var previousColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! CANH BAO: luot nop {message.SubmissionId} cham LOI (failed)");
        }
        finally
        {
            Console.ForegroundColor = previousColor;
        }
    }

    if ((message.Status == "graded" || message.Status == "failed") &&
        notifications.TryGetValue(message.SubmissionId, out var notification))
    {
        notification.TrySetResult(message.Status);
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Khong co bai tap.");
    return;
}

Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");
var requests = twoSubmissions
    ? new[]
    {
        new CreateSubmissionRequest("SV001", "https://example.com/sv001/signalr"),
        new CreateSubmissionRequest("SV002", "http://example.com/sv002/signalr")
    }
    : new[] { new CreateSubmissionRequest(studentCode, repositoryUrl) };

var createdSubmissions = new List<(string StudentCode, SubmissionDto Submission, TaskCompletionSource<string> Notification)>();
foreach (var request in requests)
{
    using var response = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions", request);

    if (response.StatusCode != HttpStatusCode.Created)
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Loi: {error?.Code} - {error?.Message}");
        return;
    }

    var created = await response.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        Console.WriteLine("Khong doc duoc luot nop vua tao.");
        return;
    }

    var notification = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    if (!notifications.TryAdd(created.Id, notification))
        throw new InvalidOperationException("Trung ID luot nop.");

    createdSubmissions.Add((request.StudentCode, created, notification));
    Console.WriteLine($"Da tao luot nop: {created.Id} ({request.StudentCode})");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
}

Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
var allNotifications = Task.WhenAll(createdSubmissions.Select(item => item.Notification.Task));
if (await Task.WhenAny(allNotifications, Task.Delay(TimeSpan.FromSeconds(10))) != allNotifications)
    Console.WriteLine("Qua thoi gian cho thong bao trang thai cuoi.");

foreach (var item in createdSubmissions)
{
    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{item.Submission.Id}");
    if (twoSubmissions)
    {
        var signalRStatus = item.Notification.Task.IsCompletedSuccessfully
            ? item.Notification.Task.Result
            : "khong nhan duoc";
        Console.WriteLine($"{item.StudentCode}: SignalR = {signalRStatus}, REST = {latest?.Status}");
    }
    else
    {
        Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
    }
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);