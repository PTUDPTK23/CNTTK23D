using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var mode = args.Length > 2 ? args[2] : "single";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
var notifications = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();
var watchedIds = new ConcurrentDictionary<Guid, byte>();

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss}");
    if ((message.Status is "graded" or "failed") && notifications.TryGetValue(message.SubmissionId, out var completion))
        completion.TrySetResult(message.Status);
});
connection.Reconnecting += _ =>
{
    Console.WriteLine("Dang ket noi lai SignalR...");
    return Task.CompletedTask;
};
connection.Reconnected += async _ =>
{
    Console.WriteLine("Da ket noi lai SignalR; dang dang ky theo doi lai.");
    foreach (var id in watchedIds.Keys)
        await connection.InvokeAsync("WatchSubmission", id.ToString());
};

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault(item => item.Status == "open");
if (selected is null)
{
    Console.WriteLine("Khong co bai tap dang mo.");
    return;
}
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

var count = mode == "--two" ? 2 : 1;
var pending = new List<SubmissionDto>();
for (var index = 0; index < count; index++)
{
    var code = count == 1 ? studentCode : $"{studentCode}-{index + 1}";
    var repositoryUrl = mode == "--fail" && index == 0
        ? $"http://example.com/{code}/signalr"
        : $"https://example.com/{code}/signalr";
    var response = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(code, repositoryUrl));
    if (response.StatusCode != HttpStatusCode.Created)
    {
        Console.WriteLine($"POST loi {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return;
    }

    var created = await response.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        Console.WriteLine("Khong doc duoc phan hoi tao luot nop.");
        return;
    }
    pending.Add(created);
    notifications[created.Id] = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    watchedIds[created.Id] = 0;
    Console.WriteLine($"Da tao luot nop {created.StudentCode}: {created.Id} ({created.Status})");
    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
}

var waits = pending.Select(item => notifications[item.Id].Task).ToArray();
var allFinished = Task.WhenAll(waits);
if (await Task.WhenAny(allFinished, Task.Delay(TimeSpan.FromSeconds(12))) != allFinished)
    Console.WriteLine("Qua thoi gian cho thong bao; se doc lai trang thai bang REST.");

foreach (var created in pending)
{
    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    var signalRStatus = notifications[created.Id].Task.IsCompleted
        ? notifications[created.Id].Task.Result
        : "khong nhan duoc";
    Console.WriteLine($"{created.StudentCode}: SignalR = {signalRStatus}, REST = {latest?.Status}");
}

record AssignmentDto(int Id, string Title, string Status);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);

