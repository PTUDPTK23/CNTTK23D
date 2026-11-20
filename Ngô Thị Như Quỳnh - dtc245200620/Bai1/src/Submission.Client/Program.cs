using System.Net.Http.Json;

var baseUrl = args.FirstOrDefault() ?? "http://localhost:5000";
using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

var assignments = await client.GetFromJsonAsync<List<Assignment>>("/api/assignments") ?? [];

Console.WriteLine("Danh sach bai tap:");
foreach (var assignment in assignments)
{
    Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
}

var request = new SubmissionRequest(
    AssignmentId: 1,
    StudentCode: "DTC245200620",
    RepositoryUrl: "https://github.com/DTC245200620/CNTTK23D");

var response = await client.PostAsJsonAsync("/api/submissions", request);
response.EnsureSuccessStatusCode();

var submission = await response.Content.ReadFromJsonAsync<SubmissionResult>()
    ?? throw new InvalidOperationException("Khong doc duoc ket qua nop bai.");

Console.WriteLine($"Da nop bai. Ma luot nop: {submission.Id}");
Console.WriteLine($"Trang thai: {submission.Status}");

var savedSubmission = await client.GetFromJsonAsync<SubmissionResult>($"/api/submissions/{submission.Id}")
    ?? throw new InvalidOperationException("Khong doc lai duoc luot nop.");

Console.WriteLine(
    $"Doc lai luot nop: {savedSubmission.Id} | bai {savedSubmission.AssignmentId} | " +
    $"{savedSubmission.StudentCode} | {savedSubmission.Status}");

record Assignment(int Id, string Title, string Description);
record SubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);
