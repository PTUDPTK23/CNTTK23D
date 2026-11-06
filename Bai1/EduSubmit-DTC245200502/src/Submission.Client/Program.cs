using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

// 1. Lấy danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<Assignment>>("/api/assignments");
Console.WriteLine("Danh sach bai tap:");
foreach (var assignment in assignments ?? new List<Assignment>()) {
    Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
}

// 2. Thực hiện nộp bài
Console.WriteLine("\nThuc hien nop bai...");
var request = new CreateSubmissionRequest(
    AssignmentId: 1,
    StudentCode: "dtc245200502",
    RepositoryUrl: "https://github.com/dtc245200502-ship-it/EduSubmit-DTC245200502");

var response = await http.PostAsJsonAsync("/api/submissions", request);
response.EnsureSuccessStatusCode();

var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
Console.WriteLine($"Da nop bai. Ma luot nop: {result?.Id}");
Console.WriteLine($"Trang thai: {result?.Status}");

// 3. Đọc lại lượt nộp vừa tạo
if (result != null) {
    Console.WriteLine("\nDoc lai luot nop vua tao:");
    var lookup = await http.GetFromJsonAsync<SubmissionResult>($"/api/submissions/{result.Id}");
    if (lookup != null) {
        Console.WriteLine($"  Ma luot nop: {lookup.Id}");
        Console.WriteLine($"  MSSV: {lookup.StudentCode}");
        Console.WriteLine($"  Trang thai: {lookup.Status}");
        Console.WriteLine($"  Thoi gian nhan: {lookup.ReceivedAt}");
    }
}

record Assignment(int Id, string Title, string Description);

record CreateSubmissionRequest(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl);

record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string Status,
    DateTimeOffset ReceivedAt);
