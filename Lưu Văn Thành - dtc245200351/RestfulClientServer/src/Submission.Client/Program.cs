using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

// Nấc 1: Liệt kê bài tập
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
Console.WriteLine("Danh sach bai tap:");
foreach (var a in assignments ?? []) {
    Console.WriteLine($"- {a.Id}: {a.Title} (Mo: {a.IsOpen})");
}

// Nấc 2: Nộp bài và kiểm tra mã 201 trước khi đọc body
var selectedAssignmentId = assignments?.FirstOrDefault(a => a.IsOpen)?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://github.com/sv001/rest-api"));

if (createResponse.StatusCode != HttpStatusCode.Created) {
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
var location = createResponse.Headers.Location;
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");
Console.WriteLine($"Location: {location}");

// Nấc 3: Đọc lại lượt nộp theo đúng địa chỉ trong Location
if (location is not null) {
    var detail = await http.GetFromJsonAsync<SubmissionDto>(location);
    Console.WriteLine($"\nDoc lai luot nop: {detail?.StudentCode} - {detail?.RepositoryUrl}");
}

// Nấc 4: Cố ý gửi sai để nhận lỗi 400 và in ApiError mà không sập chương trình
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));
Console.WriteLine($"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage r) {
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);