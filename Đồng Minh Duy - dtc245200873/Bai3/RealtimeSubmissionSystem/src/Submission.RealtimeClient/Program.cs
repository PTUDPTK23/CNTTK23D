using System.Net;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder().WithUrl($"{apiBaseUrl}/hubs/submissions").WithAutomaticReconnect().Build();
var completion = new ConcurrentDictionary<Guid, TaskCompletionSource>();
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt.ToLocalTime():HH:mm:ss}");
    if (message.Status == "failed") Console.ForegroundColor = ConsoleColor.Red;
    if (message.Status == "failed") Console.WriteLine($"!!! CANH BAO: luot nop {message.SubmissionId} cham LOI");
    if (message.Status == "failed") Console.ResetColor();
    if (message.Status is "graded" or "failed") completion.GetValueOrDefault(message.SubmissionId)?.TrySetResult();
});
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null) { Console.WriteLine("Khong co bai tap nao de nop."); return; }
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");
var requests = new[]
{
    new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode.ToLowerInvariant()}/signalr"),
    new CreateSubmissionRequest($"{studentCode}_FAIL", "http://example.com/invalid-repository")
};
var createdSubmissions = new List<SubmissionDto>();
foreach (var request in requests)
{
    var response = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions", request);
    if (response.StatusCode != HttpStatusCode.Created) { await PrintApiErrorAsync(response); continue; }
    var created = await response.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null) continue;
    createdSubmissions.Add(created);
    completion[created.Id] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    Console.WriteLine($"Da tao {created.StudentCode}: {created.Id} ({created.Status})");
    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
}
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
await Task.WhenAll(createdSubmissions.Select(async submission =>
{
    await Task.WhenAny(completion[submission.Id].Task, Task.Delay(TimeSpan.FromSeconds(10)));
    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{submission.Id}");
    Console.WriteLine($"{submission.StudentCode}: REST = {latest?.Status}");
}));

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode}: {error?.Code} - {error?.Message}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
