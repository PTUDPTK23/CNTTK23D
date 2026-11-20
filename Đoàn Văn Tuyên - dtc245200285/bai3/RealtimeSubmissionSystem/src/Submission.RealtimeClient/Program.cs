using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var twoSubmissions = args.Any(value => value.Equals("--two", StringComparison.OrdinalIgnoreCase));
var lateWatch = args.Any(value => value.Equals("--late-watch", StringComparison.OrdinalIgnoreCase));
var wrongEvent = args.Any(value => value.Equals("--wrong-event", StringComparison.OrdinalIgnoreCase));
var wrongHub = args.Any(value => value.Equals("--wrong-hub", StringComparison.OrdinalIgnoreCase));
var repositoryUrl = args.FirstOrDefault(value => value.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                                   && !value.StartsWith(apiBaseUrl, StringComparison.OrdinalIgnoreCase))
                    ?? $"https://example.com/{studentCode}/signalr";
var hubPath = wrongHub ? "/hub/submissions" : "/hubs/submissions";
var eventName = wrongEvent ? "SubmissionStatusChange" : "SubmissionStatusChanged";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}{hubPath}")
    .WithAutomaticReconnect()
    .Build();

var completedSignals = new ConcurrentDictionary<Guid, TaskCompletionSource<SubmissionStatusMessage>>();
connection.On<SubmissionStatusMessage>(eventName, message =>
{
    Console.WriteLine($"Thong bao SignalR: {Short(message.SubmissionId)} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss}");
    if (completedSignals.TryGetValue(message.SubmissionId, out var signal) &&
        message.Status is "graded" or "failed")
        signal.TrySetResult(message);
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments") ?? [];
var selected = assignments.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Khong co bai tap de nop.");
    return;
}

var requests = twoSubmissions
    ? new[]
    {
        (Code: studentCode, Url: $"https://example.com/{studentCode}/signalr"),
        (Code: "SV002", Url: "http://example.com/SV002/signalr")
    }
    : new[] { (Code: studentCode, Url: repositoryUrl) };

var submissions = new List<SubmissionDto>();
foreach (var request in requests)
{
    var response = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(request.Code, request.Url));
    if (response.StatusCode != HttpStatusCode.Created)
    {
        Console.WriteLine($"POST that bai ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        continue;
    }

    var submission = await response.Content.ReadFromJsonAsync<SubmissionDto>();
    if (submission is null)
        continue;
    submissions.Add(submission);
    completedSignals[submission.Id] = new TaskCompletionSource<SubmissionStatusMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
    Console.WriteLine($"Da tao luot nop {submission.StudentCode}: {Short(submission.Id)}");
    Console.WriteLine($"Trang thai REST ban dau: {submission.Status}");
    if (lateWatch)
    {
        Console.WriteLine("Thi nghiem: doi 4 giay truoc khi WatchSubmission.");
        await Task.Delay(TimeSpan.FromSeconds(4));
    }
    await connection.InvokeAsync("WatchSubmission", submission.Id.ToString());
}

if (submissions.Count == 0)
    return;

foreach (var submission in submissions)
{
    var signal = completedSignals[submission.Id];
    var winner = await Task.WhenAny(signal.Task, Task.Delay(TimeSpan.FromSeconds(10)));
    if (winner != signal.Task)
        Console.WriteLine($"Qua thoi gian cho thong bao graded: {submission.StudentCode}.");

    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{submission.Id}");
    Console.WriteLine($"{submission.StudentCode}: SignalR = {(signal.Task.IsCompleted ? signal.Task.Result.Status : "timeout")}, REST = {latest?.Status}");
    if (signal.Task.IsCompleted && signal.Task.Result.Status == "failed")
        Console.WriteLine($"!!! CANH BAO: luot nop {Short(submission.Id)} cham LOI (failed)");
}

static string Short(Guid id) => id.ToString("N")[..8];
record AssignmentDto(int Id, string Title, string Description);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
