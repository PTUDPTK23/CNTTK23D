using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

try
{
    // NAC 1: Liet ke bai tap
    Console.WriteLine("NAC 1 - DANH SACH BAI TAP DANG MO");
    using var listResponse = await http.GetAsync("/api/assignments");
    if (!listResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(listResponse);
        return;
    }

    var assignments = await listResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>() ?? [];
    foreach (var item in assignments.Where(x => x.IsOpen))
        Console.WriteLine($"- {item.Id}: {item.Title}");

    var selected = assignments.FirstOrDefault(x => x.IsOpen);
    if (selected is null)
    {
        Console.WriteLine("Hien khong co bai tap dang mo.");
        return;
    }

    // NAC 2: Gui yeu cau nop bai
    Console.WriteLine("\nNAC 2 - GUI YEU CAU NOP BAI");
    using var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(
        "DTC245200583",
        "https://github.com/Dtc245200583/CNTTK23D"
    )
);

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null || createResponse.Headers.Location is null)
    {
        Console.WriteLine("Khong doc duoc thong tin luot nop hoac header Location.");
        return;
    }

    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai ban dau: {created.Status}");

    // NAC 3: Doc lai theo URI trong Location
    Console.WriteLine("\nNAC 3 - DOC LAI LUOT NOP");
    using var readBack = await http.GetAsync(createResponse.Headers.Location);
    if (!readBack.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(readBack);
        return;
    }

    var reread = await readBack.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Doc lai: {reread?.StudentCode} - {reread?.RepositoryUrl}");

    // NAC 4: Co y gui du lieu sai, khong de chuong trinh bi sap
    Console.WriteLine("\nNAC 4 - THU GUI DU LIEU SAI");
    using var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest("", ""));

    Console.WriteLine($"HTTP {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"LOI KET NOI: {ex.Message}");
    Console.WriteLine("Kiem tra Submission.Api da chay tai " + apiBaseUrl + " hay chua.");
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode) return;

    // Chi doc ApiError neu response co body JSON theo hop dong cua handler.
    try
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        if (error is not null)
            Console.WriteLine($"{error.Code}: {error.Message}");
        else
            Console.WriteLine("Phan hoi loi khong co ApiError JSON.");
    }
    catch
    {
        Console.WriteLine("Phan hoi loi khong phai ApiError JSON (co the do framework tao).");
    }
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset SubmittedAt);
record ApiError(string Code, string Message);
