using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

try
{
    // Nac 1: xem danh sach bai tap dang mo
    var listResponse = await http.GetAsync("/api/assignments");
    if (!listResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(listResponse);
        return;
    }

    var assignments = await listResponse.Content
        .ReadFromJsonAsync<List<AssignmentDto>>();
    var openAssignments = assignments?.Where(item => item.IsOpen).ToList() ?? [];

    Console.WriteLine("Danh sach bai tap dang mo:");
    foreach (var assignment in openAssignments)
    {
        Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
    }

    var selectedAssignmentId = openAssignments.FirstOrDefault()?.Id ?? 1;

    // Nac 2: nop bai
    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest(
            "dtc245200620",
            "https://github.com/DTC245200620/CNTTK23D"));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content
        .ReadFromJsonAsync<SubmissionDto>();
    var location = createResponse.Headers.Location;

    Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");

    // Nac 3: doc lai bang dia chi Location
    if (location is null)
    {
        Console.WriteLine("Phan hoi khong co Location.");
        return;
    }

    var readResponse = await http.GetAsync(location);
    if (!readResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(readResponse);
        return;
    }

    var saved = await readResponse.Content
        .ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine(
        $"Doc lai luot nop: {saved?.StudentCode} - {saved?.RepositoryUrl}");

    // Nac 4: gui sai de thu loi 400
    var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest("", ""));

    Console.WriteLine(
        $"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");

    if (!invalidResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(invalidResponse);
    }
}
catch (HttpRequestException exception)
{
    Console.WriteLine($"Khong ket noi duoc API: {exception.Message}");
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);

record ApiError(string Code, string Message);
