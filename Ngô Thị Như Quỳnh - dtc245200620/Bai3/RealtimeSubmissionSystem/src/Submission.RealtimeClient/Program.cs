using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var repositoryUrl = args.Length > 2
    ? args[2]
    : $"https://example.com/{studentCode}/signalr";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var terminalNotification = new TaskCompletionSource<string>(
    TaskCreationOptions.RunContinuationsAsynchronously);
Guid? watchedSubmissionId = null;

connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged",
    message =>
    {
        Console.WriteLine(
            $"Thong bao SignalR: {message.SubmissionId} -> {message.Status} "
            + $"luc {message.UpdatedAt:HH:mm:ss}");

        if (message.Status == "failed")
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"!!! CANH BAO: luot nop {message.SubmissionId} cham LOI (failed)");
            Console.ForegroundColor = originalColor;
        }

        if (message.Status is "graded" or "failed")
        {
            terminalNotification.TrySetResult(message.Status);
        }
    });

connection.Reconnected += async _ =>
{
    if (watchedSubmissionId is Guid submissionId)
    {
        await connection.InvokeAsync(
            "WatchSubmission",
            submissionId.ToString());
    }
};

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>(
    "/api/assignments");
var selected = assignments?.FirstOrDefault(item => item.Status == "open");

if (selected is null)
{
    Console.WriteLine("Khong co bai tap dang mo.");
    return;
}

Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(
        studentCode,
        repositoryUrl));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine(
        $"Khong tao duoc luot nop: {error?.Code} - {error?.Message}");
    return;
}

var created = await createResponse.Content
    .ReadFromJsonAsync<SubmissionDto>();

if (created is null)
{
    Console.WriteLine("Phan hoi tao luot nop khong hop le.");
    return;
}

watchedSubmissionId = created.Id;
Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

await connection.InvokeAsync(
    "WatchSubmission",
    created.Id.ToString());

Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

var completed = await Task.WhenAny(
    terminalNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != terminalNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

var latest = await http.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{created.Id}");
Console.WriteLine(
    $"Trang thai doc lai bang REST: {latest?.Status}");

record AssignmentDto(int Id, string Title, string Status);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

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
