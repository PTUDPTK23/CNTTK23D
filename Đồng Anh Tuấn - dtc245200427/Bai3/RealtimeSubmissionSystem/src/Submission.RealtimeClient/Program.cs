using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

try
{
    await RunAsync(apiBaseUrl, studentCode);
    return 0;
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"HttpRequestException: {ex.Message}");
    return 1;
}
catch (HubException ex)
{
    Console.Error.WriteLine($"HubException: {ex.Message}");
    return 1;
}

static async Task RunAsync(string apiBaseUrl, string studentCode)
{
    using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    await using var connection = new HubConnectionBuilder()
        .WithUrl($"{apiBaseUrl}/hubs/submissions")
        .WithAutomaticReconnect()
        .Build();

    var gradedNotification = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
    {
        Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss}");
        if (message.Status == "graded")
            gradedNotification.TrySetResult();
    });

    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault(item => item.IsOpen);
    if (selected is null)
    {
        Console.WriteLine("Khong co bai tap dang mo.");
        return;
    }

    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");
    var repository = $"https://example.com/{Uri.EscapeDataString(studentCode)}/signalr";
    using var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(studentCode, repository));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.Error.WriteLine($"HTTP {(int)createResponse.StatusCode}: {error?.Code} {error?.Message}");
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>()
        ?? throw new HttpRequestException("May chu khong tra ve luot nop.");

    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    var completed = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
    if (completed != gradedNotification.Task)
        Console.WriteLine("Qua thoi gian cho thong bao graded.");

    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
