using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

var options = args.Where(arg => arg.StartsWith("--", StringComparison.Ordinal)).ToHashSet();
var positional = args.Where(arg => !arg.StartsWith("--", StringComparison.Ordinal)).ToArray();
var apiBase = (positional.ElementAtOrDefault(0) ?? "http://localhost:5000").TrimEnd('/');
var studentCode = positional.ElementAtOrDefault(1) ?? "DTC245200604";
if (positional.Length > 2 || options.Except(["--wrong-hub", "--wrong-event", "--late-watch", "--bonus"]).Any() ||
    !Uri.TryCreate(apiBase, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
{
    Console.Error.WriteLine("Usage: Submission.RealtimeClient [API URL] [student code] [--wrong-hub|--wrong-event|--late-watch|--bonus]");
    return 2;
}

using var http = new HttpClient { BaseAddress = baseUri };
var hubPath = options.Contains("--wrong-hub") ? "/hub/submissions" : "/hubs/submissions";
var eventName = options.Contains("--wrong-event") ? "SubmissionStatusChange" : "SubmissionStatusChanged";
await using var connection = new HubConnectionBuilder()
    .WithUrl(apiBase + hubPath)
    .WithAutomaticReconnect()
    .Build();

var pending = new ConcurrentDictionary<Guid, PendingSubmission>();
using var subscription = connection.On<SubmissionStatusMessage>(eventName, message =>
{
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss.fff} UTC");
    if (pending.TryGetValue(message.SubmissionId, out var watched))
        CompleteIfTerminal(watched, message.Status);
});

connection.Reconnected += async _ =>
{
    Console.WriteLine("Da ket noi lai SignalR.");
    foreach (var watched in pending.Values)
    {
        try
        {
            await WatchAsync(watched);
            var latest = await http.GetFromJsonAsync<SubmissionDto>(watched.Location);
            Console.WriteLine($"Trang thai REST sau khi ket noi lai: {latest?.Status} ({watched.Id})");
            CompleteIfTerminal(watched, latest?.Status);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Khong dong bo duoc sau khi ket noi lai: {ex.Message}");
        }
    }
};

try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault(item => item.IsOpen);
    if (selected is null)
    {
        Console.Error.WriteLine("Khong co bai tap dang mo.");
        return 1;
    }
    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

    var watch = await SubmitAndWatchAsync(selected.Id, studentCode, "https");
    if (watch is null) return 1;
    var submissions = new List<PendingSubmission> { watch };
    if (options.Contains("--bonus"))
    {
        var second = await SubmitAndWatchAsync(selected.Id, $"{studentCode}-bonus", "http");
        if (second is null) return 1;
        submissions.Add(second);
    }

    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    try
    {
        await Task.WhenAll(submissions.Select(item => item.Completed.Task)).WaitAsync(TimeSpan.FromSeconds(10));
    }
    catch (TimeoutException)
    {
        Console.WriteLine("Qua thoi gian cho thong bao graded.");
    }

    foreach (var submission in submissions)
    {
        var latest = await http.GetFromJsonAsync<SubmissionDto>(submission.Location);
        if (submissions.Count > 1) Console.WriteLine($"Luot nop {submission.Id} ({submission.StudentCode})");
        Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
        CompleteIfTerminal(submission, latest?.Status);
    }
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
    return 1;
}

async Task<PendingSubmission?> SubmitAndWatchAsync(int assignmentId, string code, string scheme)
{
    using var response = await http.PostAsJsonAsync($"/api/assignments/{assignmentId}/submissions",
        new CreateSubmissionRequest(code, $"{scheme}://example.com/{Uri.EscapeDataString(code)}/signalr"));
    if (response.StatusCode != HttpStatusCode.Created)
    {
        var error = response.Content.Headers.ContentType?.MediaType == "application/json"
            ? await response.Content.ReadFromJsonAsync<ApiError>() : null;
        Console.Error.WriteLine($"POST that bai ({(int)response.StatusCode}): {error?.Code} {error?.Message}");
        return null;
    }

    var created = await response.Content.ReadFromJsonAsync<SubmissionDto>()
        ?? throw new JsonException("POST 201 did not contain a submission.");
    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
    var watch = new PendingSubmission(created.Id, created.StudentCode,
        response.Headers.Location ?? new Uri($"/api/submissions/{created.Id}", UriKind.Relative));
    if (options.Contains("--late-watch")) await Task.Delay(TimeSpan.FromSeconds(4));
    pending[watch.Id] = watch;
    await WatchAsync(watch);
    return watch;
}

async Task WatchAsync(PendingSubmission submission)
{
    try
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await connection.InvokeAsync("WatchSubmission", submission.Id.ToString(), timeout.Token);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Khong goi duoc WatchSubmission; se doc REST: {ex.Message}");
    }
}

static void CompleteIfTerminal(PendingSubmission submission, string? status)
{
    if (status == "graded") submission.Completed.TrySetResult(status);
    if (status != "failed") return;
    lock (submission)
    {
        if (submission.Completed.Task.IsCompleted) return;
        var previous = Console.ForegroundColor;
        try
        {
            if (!Console.IsOutputRedirected) Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! CANH BAO: luot nop {submission.Id} cham LOI (failed)");
        }
        finally
        {
            if (!Console.IsOutputRedirected) Console.ForegroundColor = previous;
        }
        submission.Completed.TrySetResult(status);
    }
}

sealed record PendingSubmission(Guid Id, string StudentCode, Uri Location)
{
    public TaskCompletionSource<string> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
