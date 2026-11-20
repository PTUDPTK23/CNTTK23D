using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiUrl = args.FirstOrDefault() ?? "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiUrl) };

Console.WriteLine($"REST: {apiUrl}");
Console.WriteLine($"SignalR: {apiUrl}/hubs/submissions");

using var response = await http.PostAsJsonAsync(
    "/api/assignments/1/submissions",
    new CreateSubmissionRequest("dtc245200599", "https://github.com/example/edusubmit"));
response.EnsureSuccessStatusCode();

var submission = await response.Content.ReadFromJsonAsync<SubmissionDto>()
    ?? throw new InvalidOperationException("API khong tra ve luot nop.");
Console.WriteLine($"Da nop: {submission.Id} | trang thai REST: {submission.Status}");

var graded = new TaskCompletionSource<SubmissionStatusEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
var grading = new TaskCompletionSource<SubmissionStatusEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiUrl.TrimEnd('/')}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

connection.On<SubmissionStatusEvent>("SubmissionStatusChanged", update =>
{
    Console.WriteLine($"Chuông: {update.Status} | UpdatedAt: {update.UpdatedAt:O}");
    if (string.Equals(update.Status, "graded", StringComparison.OrdinalIgnoreCase))
    {
        graded.TrySetResult(update);
    }
    else if (string.Equals(update.Status, "grading", StringComparison.OrdinalIgnoreCase))
    {
        grading.TrySetResult(update);
    }
});

await connection.StartAsync();
await connection.InvokeAsync("JoinSubmissionGroup", submission.Id);
Console.WriteLine($"SignalR da vao nhom luot nop {submission.Id}; dang cho graded...");

await Task.WhenAll(
    grading.Task.WaitAsync(TimeSpan.FromSeconds(20)),
    graded.Task.WaitAsync(TimeSpan.FromSeconds(20)));
Console.WriteLine("Da nhan 2 cap nhat tu may chu, client khong hoi REST trong luc cho.");

using var finalResponse = await http.GetAsync($"/api/submissions/{submission.Id}");
finalResponse.EnsureSuccessStatusCode();
var finalSubmission = await finalResponse.Content.ReadFromJsonAsync<SubmissionDto>()
    ?? throw new InvalidOperationException("Khong doc duoc trang thai cuoi qua REST.");
Console.WriteLine($"REST chot lai: {finalSubmission.Status} | UpdatedAt: {finalSubmission.UpdatedAt:O}");

record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, string Status, DateTimeOffset UpdatedAt);
record SubmissionStatusEvent(string Status, DateTimeOffset UpdatedAt);
