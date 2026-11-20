using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "dtc245200173";
var repositoryUrl = args.Length > 2 ? args[2] : DemoRepository(studentCode);
var requests = new List<CreateSubmissionRequest> { new(studentCode, repositoryUrl) };
if (args.Length > 3)
{
    var secondStudent = args[3];
    requests.Add(new(secondStudent, args.Length > 4 ? args[4] : DemoRepository(secondStudent)));
}

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var pending = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    if (!pending.TryGetValue(message.SubmissionId, out var completion))
        return;

    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt.ToLocalTime():HH:mm:ss}");
    if (message.Status == "failed")
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"CANH BAO: luot nop {message.SubmissionId} cham loi (failed).");
        Console.ForegroundColor = previousColor;
    }

    if (IsFinal(message.Status))
        completion.TrySetResult(message.Status);
});

connection.Reconnecting += _ =>
{
    Console.WriteLine("Mat ket noi SignalR. Dang ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += async _ =>
{
    foreach (var (id, completion) in pending)
    {
        try
        {
            await WatchAsync(id);
            var latest = await ReadSubmissionAsync(id);
            Console.WriteLine($"Trang thai REST sau khi ket noi lai: {id} -> {latest.Status}");
            if (IsFinal(latest.Status))
                completion.TrySetResult(latest.Status);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Khong khoi phuc duoc theo doi {id}: {error.Message}");
        }
    }
};

try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault(assignment => assignment.IsOpen);
    if (selected is null)
    {
        Console.Error.WriteLine("Khong co bai tap dang mo.");
        return 1;
    }

    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");
    var createdSubmissions = new List<SubmissionDto>();
    foreach (var request in requests)
    {
        using var response = await http.PostAsJsonAsync(
            $"/api/assignments/{selected.Id}/submissions", request);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Console.Error.WriteLine($"HTTP {(int)response.StatusCode}: {error?.Code} - {error?.Message}");
            return 1;
        }

        var created = await response.Content.ReadFromJsonAsync<SubmissionDto>()
            ?? throw new InvalidOperationException("Phan hoi POST khong co du lieu luot nop.");
        createdSubmissions.Add(created);
        pending[created.Id] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var suffix = requests.Count > 1 ? $" ({created.StudentCode})" : "";
        Console.WriteLine($"Da tao luot nop: {created.Id}{suffix}");
        Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
        await WatchAsync(created.Id);
    }

    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    try
    {
        await Task.WhenAll(pending.Values.Select(completion => completion.Task))
            .WaitAsync(TimeSpan.FromSeconds(10));
    }
    catch (TimeoutException)
    {
        Console.WriteLine("Qua thoi gian cho thong bao graded hoac failed.");
    }

    var allFinal = true;
    foreach (var created in createdSubmissions)
    {
        var latest = await ReadSubmissionAsync(created.Id);
        var prefix = createdSubmissions.Count > 1 ? $"{created.StudentCode} ({created.Id}): " : "";
        Console.WriteLine($"{prefix}Trang thai doc lai bang REST: {latest.Status}");
        allFinal &= IsFinal(latest.Status);
    }

    return allFinal ? 0 : 1;
}
catch (Exception error)
{
    Console.Error.WriteLine($"{error.GetType().Name}: {error.Message}");
    return 1;
}

Task WatchAsync(Guid id) => connection.InvokeAsync("WatchSubmission", id);

async Task<SubmissionDto> ReadSubmissionAsync(Guid id) =>
    await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{id}")
    ?? throw new InvalidOperationException("Phan hoi GET khong co du lieu luot nop.");

static string DemoRepository(string code) => $"https://example.com/{Uri.EscapeDataString(code)}/signalr";
static bool IsFinal(string status) => status is "graded" or "failed";

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, string StudentCode, string Status);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
