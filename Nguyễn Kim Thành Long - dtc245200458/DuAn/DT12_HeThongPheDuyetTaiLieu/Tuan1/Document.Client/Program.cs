using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Document.Client.Models;

Console.OutputEncoding = Encoding.UTF8;

var apiBaseUrl = Environment.GetEnvironmentVariable("DOCUMENT_API_URL");
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    apiBaseUrl = "http://localhost:5001";
}

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    Converters = { new JsonStringEnumConverter() }
};

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/")
};

Console.WriteLine("=================================================================");
Console.WriteLine("  DEMO TUẦN 1 - HỆ THỐNG PHÊ DUYỆT TÀI LIỆU PHÂN TÁN (CLIENT)");
Console.WriteLine($"  API Base URL: {httpClient.BaseAddress}");
Console.WriteLine("=================================================================\n");

try
{
    // (1) Tạo 2 tài liệu mới
    Console.WriteLine("[1] TẠO 2 TÀI LIỆU MỚI (POST /documents)");
    Console.WriteLine("-----------------------------------------------------------------");

    var newDocs = new[]
    {
        new CreateDocumentRequestDto(
            Title: "Tờ trình nâng cấp hạ tầng mạng",
            Content: "Đề nghị nâng cấp switch core và đường truyền dự phòng cho tòa nhà A.",
            Creator: "nguyen.kim.thanh.long",
            Approver: "truong.phong.it"),
        new CreateDocumentRequestDto(
            Title: "Quy trình sao lưu dữ liệu định kỳ",
            Content: "Áp dụng chiến lược backup 3-2-1 cho toàn bộ cơ sở dữ liệu sản xuất.",
            Creator: "nguyen.kim.thanh.long",
            Approver: "giam.doc.ky.thuat")
    };

    var createdDocuments = new List<DocumentItemDto>();

    foreach (var request in newDocs)
    {
        var response = await httpClient.PostAsJsonAsync("documents", request, jsonOptions);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<DocumentItemDto>(jsonOptions)
            ?? throw new InvalidOperationException("Không đọc được dữ liệu tài liệu trả về.");

        createdDocuments.Add(created);

        Console.WriteLine($"-> Đã tạo thành công (HTTP {(int)response.StatusCode}):");
        Console.WriteLine($"   Id       : {created.Id}");
        Console.WriteLine($"   Title    : {created.Title}");
        Console.WriteLine($"   Creator  : {created.Creator}");
        Console.WriteLine($"   Approver : {created.Approver}");
        Console.WriteLine($"   Status   : {created.Status}");
        Console.WriteLine($"   CreatedAt: {created.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}\n");
    }

    // (2) Lấy danh sách tài liệu
    Console.WriteLine("[2] LẤY DANH SÁCH TÀI LIỆU (GET /documents)");
    Console.WriteLine("-----------------------------------------------------------------");

    var allDocuments = await httpClient.GetFromJsonAsync<List<DocumentItemDto>>("documents", jsonOptions)
        ?? [];

    Console.WriteLine($"Tổng số tài liệu hiện có: {allDocuments.Count}\n");
    for (var i = 0; i < allDocuments.Count; i++)
    {
        var doc = allDocuments[i];
        Console.WriteLine($"{i + 1}. [{doc.Status}] {doc.Title}");
        Console.WriteLine($"   Id: {doc.Id} | Creator: {doc.Creator} | Approver: {doc.Approver}");
    }
    Console.WriteLine();

    // (3) Xem chi tiết 1 tài liệu kèm lịch sử xử lý
    var targetDocId = createdDocuments[0].Id;
    Console.WriteLine($"[3] XEM CHI TIẾT TÀI LIỆU KÈM LỊCH SỬ (GET /documents/{targetDocId})");
    Console.WriteLine("-----------------------------------------------------------------");

    var detail = await httpClient.GetFromJsonAsync<DocumentDetailDto>($"documents/{targetDocId}", jsonOptions)
        ?? throw new InvalidOperationException("Không đọc được chi tiết tài liệu.");

    Console.WriteLine($"Id        : {detail.Id}");
    Console.WriteLine($"Title     : {detail.Title}");
    Console.WriteLine($"Content   : {detail.Content}");
    Console.WriteLine($"Creator   : {detail.Creator}");
    Console.WriteLine($"Approver  : {detail.Approver}");
    Console.WriteLine($"Status    : {detail.Status}");
    Console.WriteLine($"CreatedAt : {detail.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}");
    Console.WriteLine($"UpdatedAt : {detail.UpdatedAt:yyyy-MM-dd HH:mm:ss zzz}");
    Console.WriteLine($"Lịch sử xử lý ({detail.History.Count} bản ghi):");

    foreach (var entry in detail.History)
    {
        Console.WriteLine($"  - [{entry.Timestamp:yyyy-MM-dd HH:mm:ss zzz}] Action: {entry.Action} | By: {entry.PerformedBy}");
        Console.WriteLine($"    Note: {entry.Note} (HistoryId: {entry.Id})");
    }

    Console.WriteLine("\n=================================================================");
    Console.WriteLine("  HOÀN THÀNH DEMO TUẦN 1.");
    Console.WriteLine("=================================================================");
}
catch (HttpRequestException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[LỖI KẾT NỐI API] Không thể kết nối tới {httpClient.BaseAddress}");
    Console.WriteLine($"Chi tiết: {ex.Message}");
    Console.WriteLine("Vui lòng chạy Document.Api trước bằng lệnh: dotnet run --project Document.Api");
    Console.ResetColor();
    Environment.ExitCode = 1;
}
