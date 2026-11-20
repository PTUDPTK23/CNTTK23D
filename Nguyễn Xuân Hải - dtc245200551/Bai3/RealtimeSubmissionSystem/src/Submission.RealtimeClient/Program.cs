
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

const string apiBaseUrl = "http://localhost:5001";
const string hubUrl = apiBaseUrl + "/hubs/submissions";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var completed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
Guid watchedSubmissionId = Guid.Empty;

Console.WriteLine("=== Submission.RealtimeClient ===");
Console.WriteLine($"API: {apiBaseUrl}");
Console.WriteLine($"Hub: {hubUrl}");

await using var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

// Dang ky su kien truoc StartAsync de tranh bo lo thong bao.
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    if (message.SubmissionId != watchedSubmissionId)
        return;

    Console.WriteLine($"[SignalR] {message.SubmissionId} -> {message.Status} at {message.UpdatedAt:O}");
    if (message.Status is "graded" or "failed")
        completed.TrySetResult(message.Status);
});

try
{
    Console.WriteLine("\n[1] Lay danh sach bai tap:");
    using var listResponse = await http.GetAsync("/api/assignments");
    Console.WriteLine($"HTTP {(int)listResponse.StatusCode} {listResponse.StatusCode}");
    var assignments = await listResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>(jsonOptions);
    if (assignments is not null)
    {
        foreach (var assignment in assignments)
            Console.WriteLine($"- {assignment.Id}: {assignment.Title} | Mo: {assignment.IsOpen}");
    }

    Console.WriteLine("\n[2] Ket noi SignalR:");
    await connection.StartAsync();
    Console.WriteLine("SignalR connected.");

    Console.WriteLine("\n[3] Gui bai tap qua REST:");
    var request = new CreateSubmissionRequest(
        "dtc245200551",
        "https://github.com/example/student-assignment");

    using var postResponse = await http.PostAsJsonAsync(
        "/api/assignments/1/submissions", request, jsonOptions);

    Console.WriteLine($"HTTP {(int)postResponse.StatusCode} {postResponse.StatusCode}");
    if (postResponse.StatusCode != HttpStatusCode.Created)
    {
        var error = await postResponse.Content.ReadAsStringAsync();
        Console.WriteLine(error);
        return;
    }

    var created = await postResponse.Content.ReadFromJsonAsync<SubmissionDto>(jsonOptions);
    if (created is null)
    {
        Console.WriteLine("Khong doc duoc du lieu submission.");
        return;
    }

    watchedSubmissionId = created.Id;
    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai ban dau: {created.Status}");

    Console.WriteLine("\n[4] Dang ky theo doi group:");
    await connection.InvokeAsync("WatchSubmission", created.Id);
    Console.WriteLine($"Dang theo doi submission:{created.Id}");

    Console.WriteLine("\n[5] Cho thong bao trang thai (toi da 10 giay):");
    var finished = await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(10)));
    if (finished == completed.Task)
        Console.WriteLine($"Da nhan trang thai ket thuc: {await completed.Task}");
    else
        Console.WriteLine("Timeout: chua nhan duoc trang thai ket thuc.");

    Console.WriteLine("\n[6] Doc trang thai moi nhat bang REST:");
    using var getResponse = await http.GetAsync($"/api/submissions/{created.Id}");
    Console.WriteLine($"HTTP {(int)getResponse.StatusCode} {getResponse.StatusCode}");
    var latest = await getResponse.Content.ReadFromJsonAsync<SubmissionDto>(jsonOptions);
    Console.WriteLine($"Trang thai REST: {latest?.Status}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Loi HTTP/ket noi: {ex.Message}");
    Console.WriteLine("Kiem tra Realtime API co dang chay o cong 5001 khong.");
}
catch (Exception ex)
{
    Console.WriteLine($"Loi: {ex.Message}");
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
    DateTimeOffset? UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
