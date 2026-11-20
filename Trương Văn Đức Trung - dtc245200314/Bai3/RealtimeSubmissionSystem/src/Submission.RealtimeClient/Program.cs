using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var apiBaseUrl = args.FirstOrDefault(value => !value.StartsWith("--", StringComparison.Ordinal))
    ?.TrimEnd('/') ?? "http://localhost:5000";
var studentCode = args.Skip(1).FirstOrDefault(value => !value.StartsWith("--", StringComparison.Ordinal))
    ?? "DTC245200314";
var repositoryUrl = args.Skip(2).FirstOrDefault(value => !value.StartsWith("--", StringComparison.Ordinal))
    ?? "https://github.com/dtc245200314-del/CNTTK23D";
var watchTwo = args.Contains("--watch-two", StringComparer.OrdinalIgnoreCase);
var failSecond = args.Contains("--fail-second", StringComparer.OrdinalIgnoreCase);
var lateWatch = args.Contains("--late-watch", StringComparer.OrdinalIgnoreCase);
var wrongHubPath = args.Contains("--wrong-hub-path", StringComparer.OrdinalIgnoreCase);
var wrongEventName = args.Contains("--wrong-event-name", StringComparer.OrdinalIgnoreCase);

if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseAddress) ||
    (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
{
    Console.Error.WriteLine("Dia chi API phai la URL HTTP hoac HTTPS hop le.");
    return 1;
}

using var http = new HttpClient
{
    BaseAddress = baseAddress,
    Timeout = TimeSpan.FromSeconds(15)
};
var hubUrl = wrongHubPath
    ? $"{apiBaseUrl}/hub/submissions"
    : $"{apiBaseUrl}/hubs/submissions";
var eventName = wrongEventName ? "SubmissionStatusChange" : "SubmissionStatusChanged";
await using var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

var completionBySubmission = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();
connection.On<SubmissionStatusMessage>(eventName, message =>
{
    Console.WriteLine(
        $"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss}");

    if (message.Status is "graded" or "failed" &&
        completionBySubmission.TryGetValue(message.SubmissionId, out var completion))
    {
        completion.TrySetResult(message.Status);
    }
});

connection.Reconnecting += error =>
{
    Console.WriteLine($"Dang ket noi lai... {error?.Message}");
    return Task.CompletedTask;
};

connection.Reconnected += async _ =>
{
    Console.WriteLine("Da ket noi lai.");
    foreach (var id in completionBySubmission.Keys)
        await connection.InvokeAsync("WatchSubmission", id.ToString("D"));
};

try
{
    Console.WriteLine($"SignalR hub: {hubUrl}");
    Console.WriteLine($"Su kien dang nghe: {eventName}");
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments")
        ?? throw new InvalidOperationException("API khong tra ve danh sach bai tap.");
    var assignment = assignments.FirstOrDefault(item => item.IsOpen)
        ?? throw new InvalidOperationException("Khong co bai tap nao dang mo de nop.");
    Console.WriteLine($"Chon bai tap: {assignment.Id} - {assignment.Title}");

    var count = watchTwo ? 2 : 1;
    var createdSubmissions = new List<SubmissionDto>();
    for (var index = 0; index < count; index++)
    {
        var code = count == 1 ? studentCode : $"{studentCode}-{index + 1:00}";
        var url = failSecond && index == 1
            ? "http://example.com/khong-co-https"
            : repositoryUrl;
        using var response = await http.PostAsJsonAsync(
            $"/api/assignments/{assignment.Id}/submissions",
            new CreateSubmissionRequest(code, url));

        Console.WriteLine(
            $"POST /api/assignments/{assignment.Id}/submissions -> {(int)response.StatusCode} {response.StatusCode}");
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Console.Error.WriteLine($"{error?.Code}: {error?.Message}");
            return 1;
        }

        var created = await response.Content.ReadFromJsonAsync<SubmissionDto>()
            ?? throw new InvalidOperationException("API khong tra ve luot nop.");
        createdSubmissions.Add(created);
        completionBySubmission[created.Id] =
            new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Console.WriteLine($"Da tao luot nop: {created.Id}");
        Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
    }

    if (lateWatch)
        await Task.Delay(TimeSpan.FromSeconds(4));

    foreach (var submission in createdSubmissions)
        await connection.InvokeAsync("WatchSubmission", submission.Id.ToString("D"));

    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    var allFinalStatuses = Task.WhenAll(completionBySubmission.Values.Select(item => item.Task));
    var completed = await Task.WhenAny(allFinalStatuses, Task.Delay(TimeSpan.FromSeconds(10)));
    if (completed != allFinalStatuses)
        Console.WriteLine("Qua thoi gian cho thong bao trang thai.");

    foreach (var submission in createdSubmissions)
    {
        var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{submission.Id}")
            ?? throw new InvalidOperationException($"Khong doc lai duoc luot nop {submission.Id}.");
        Console.WriteLine($"Trang thai doc lai bang REST: {latest.Status}");
        if (latest.Status == "failed")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! CANH BAO: luot nop {latest.Id} cham LOI (failed)");
            Console.ResetColor();
        }
    }

    return 0;
}
catch (HttpRequestException exception)
{
    Console.Error.WriteLine($"Khong ket noi duoc API: {exception.Message}");
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Loi Client: {exception.Message}");
    return 1;
}

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
