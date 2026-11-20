using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var extraArgs = args.Skip(2).ToArray();
var runTwoSubmissions = extraArgs.Any(value => value.Equals("--two", StringComparison.OrdinalIgnoreCase));
var forceFailure = extraArgs.Any(value => value.Equals("--fail", StringComparison.OrdinalIgnoreCase));
var useWrongHub = extraArgs.Any(value => value.Equals("--wrong-hub", StringComparison.OrdinalIgnoreCase));
var useWrongEvent = extraArgs.Any(value => value.Equals("--bad-event", StringComparison.OrdinalIgnoreCase));
var watchLate = extraArgs.Any(value => value.Equals("--watch-late", StringComparison.OrdinalIgnoreCase));
var hubPath = useWrongHub ? "/hub/submissions" : "/hubs/submissions";
var statusEventName = useWrongEvent ? "SubmissionStatusChange" : "SubmissionStatusChanged";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}{hubPath}")
    .WithAutomaticReconnect()
    .Build();

var completionBySubmission = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();
connection.On<SubmissionStatusMessage>(statusEventName, message =>
{
    Console.WriteLine(
        $"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss}");

    if (message.Status is "graded" or "failed" &&
        completionBySubmission.TryGetValue(message.SubmissionId, out var completion))
    {
        completion.TrySetResult(message.Status);
    }

    if (message.Status == "failed")
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {message.SubmissionId} cham LOI (failed)");
        Console.ForegroundColor = oldColor;
    }
});

try
{
    await connection.StartAsync();
}
catch (HttpRequestException exception)
{
    Console.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}");
    return;
}

Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selectedAssignment = assignments?.FirstOrDefault(assignment => assignment.IsOpen);
if (selectedAssignment is null)
{
    Console.Error.WriteLine("Khong tim thay bai tap dang mo.");
    return;
}

var studentCodes = runTwoSubmissions
    ? new[] { studentCode, studentCode == "SV002" ? "SV001" : "SV002" }
    : new[] { studentCode };
var createdSubmissions = new List<SubmissionDto>();

for (var index = 0; index < studentCodes.Length; index++)
{
    var currentStudent = studentCodes[index];
    var shouldFail = forceFailure || (runTwoSubmissions && index == 1);
    var repositoryUrl = shouldFail
        ? $"http://example.com/{currentStudent}/signalr"
        : $"https://example.com/{currentStudent}/signalr";

    using var response = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignment.Id}/submissions",
        new CreateSubmissionRequest(currentStudent, repositoryUrl));

    if (response.StatusCode != HttpStatusCode.Created)
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Console.Error.WriteLine(
            $"REST loi {(int)response.StatusCode}: {error?.Code} - {error?.Message}");
        return;
    }

    var created = await response.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        Console.Error.WriteLine("REST khong tra ve thong tin luot nop.");
        return;
    }

    createdSubmissions.Add(created);
    completionBySubmission[created.Id] = new TaskCompletionSource<string>(
        TaskCreationOptions.RunContinuationsAsynchronously);

    Console.WriteLine($"Da tao luot nop {currentStudent}: {created.Id} ({created.Status})");
    if (index == 0)
    {
        Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
    }

    if (watchLate)
    {
        Console.WriteLine("Cho 4 giay truoc khi WatchSubmission...");
        await Task.Delay(TimeSpan.FromSeconds(4));
    }

    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
}

Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
var allFinalStates = Task.WhenAll(completionBySubmission.Values.Select(source => source.Task));
var finished = await Task.WhenAny(allFinalStates, Task.Delay(TimeSpan.FromSeconds(10)));
if (finished != allFinalStates)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

foreach (var created in createdSubmissions)
{
    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    var signalRStatus = completionBySubmission[created.Id].Task.IsCompletedSuccessfully
        ? completionBySubmission[created.Id].Task.Result
        : "timeout";
    Console.WriteLine(
        $"{created.StudentCode}: SignalR = {signalRStatus}, REST = {latest?.Status}");
}

if (createdSubmissions.Count == 1)
{
    var finalSubmission = await http.GetFromJsonAsync<SubmissionDto>(
        $"/api/submissions/{createdSubmissions[0].Id}");
    Console.WriteLine($"Trang thai doc lai bang REST: {finalSubmission?.Status}");
}

record AssignmentDto(int Id, string Title, bool IsOpen);
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
