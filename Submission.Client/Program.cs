using System.Net.Http.Json;

using var client = new HttpClient();
client.BaseAddress = new Uri(args.Length > 0 ? args[0] : "http://localhost:5000");

Console.WriteLine("Danh sach bai tap:");
var assignments = await client.GetFromJsonAsync<Assignment[]>("/api/assignments");
if (assignments != null)
{
    foreach (var a in assignments)
    {
        Console.WriteLine($"- {a.Id}: {a.Title}");
    }
}

Console.WriteLine("\nThuc hien nop bai...");
var request = new CreateSubmissionRequest(1, "dtc245200173", "https://github.com/dtc245200173/bai1");
var response = await client.PostAsJsonAsync("/api/submissions", request);

response.EnsureSuccessStatusCode();

var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
if (result != null)
{
    Console.WriteLine($"Da nop bai. Ma luot nop: {result.Id}");
    Console.WriteLine($"Trang thai: {result.Status}");
}

record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);