using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var submissionCreated = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    if (!submissionCreated.Task.IsCompletedSuccessfully || message.SubmissionId != submissionCreated.Task.Result)
        return;

    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss}");
    if (message.Status == "graded")
        gradedNotification.TrySetResult();
});

connection.Reconnected += async _ =>
{
    if (!submissionCreated.Task.IsCompletedSuccessfully)
        return;

    var id = submissionCreated.Task.Result;
    try
    {
        await connection.InvokeAsync("WatchSubmission", id.ToString());
        var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{id}");
        if (latest?.Status == "graded")
            gradedNotification.TrySetResult();
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Khong the khoi phuc theo doi: {exception.Message}");
    }
};

try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<AssignmentDto[]>("/api/assignments");
    var selected = assignments?.FirstOrDefault(assignment => assignment.IsOpen);
    if (selected is null)
    {
        Console.Error.WriteLine("Khong co bai tap dang mo.");
        return 1;
    }

    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");
    using var response = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(studentCode, $"https://example.com/{Uri.EscapeDataString(studentCode)}/signalr"));
    if (response.StatusCode != HttpStatusCode.Created)
    {
        Console.Error.WriteLine($"Tao luot nop that bai ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        return 1;
    }

    var created = await response.Content.ReadFromJsonAsync<SubmissionDto>()
        ?? throw new InvalidOperationException("Phan hoi tao luot nop rong.");
    submissionCreated.SetResult(created.Id);
    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

    try
    {
        await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
    }
    catch (InvalidOperationException) when (connection.State != HubConnectionState.Connected)
    {
        // Reconnected will renew the subscription once the connection is back.
    }

    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    var completed = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
    if (completed != gradedNotification.Task)
        Console.WriteLine("Qua thoi gian cho thong bao graded.");

    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
    return latest?.Status == "graded" ? 0 : 1;
}
catch (Exception exception) when (exception is HttpRequestException or HubException or TaskCanceledException)
{
    Console.Error.WriteLine($"EduSubmit that bai: {exception.Message}");
    return 1;
}

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, string Status);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
