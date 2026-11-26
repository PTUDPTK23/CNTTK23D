using System.Net;
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

Console.WriteLine("========================================================================");
Console.WriteLine("  DEMO TỔNG HỢP (TUẦN 2-3-4) - HỆ THỐNG PHÊ DUYỆT TÀI LIỆU PHÂN TÁN");
Console.WriteLine($"  API Base URL: {httpClient.BaseAddress}");
Console.WriteLine("========================================================================\n");

try
{
    // 1. Tạo 2 tài liệu mới (Draft)
    Console.WriteLine("[1] TẠO 2 TÀI LIỆU MỚI Ở TRẠNG THÁI DRAFT (POST /documents)");
    Console.WriteLine("------------------------------------------------------------------------");

    var doc1 = await CreateDocumentAsync(
        new CreateDocumentRequestDto(
            "Đề án chuyển đổi số phòng Hành chính",
            "Triển khai quy trình ký duyệt văn bản điện tử toàn đơn vị.",
            "nguyen.kim.thanh.long",
            "truong.phong.hc"),
        "demo-create-1");

    var doc2 = await CreateDocumentAsync(
        new CreateDocumentRequestDto(
            "Dự toán kinh phí hội thảo khoa học",
            "Chi phí tổ chức hội thảo và in ấn kỷ yếu.",
            "nguyen.kim.thanh.long",
            "ke.toan.truong"),
        "demo-create-2");

    PrintDocumentSummary("Tài liệu 1", doc1);
    PrintDocumentSummary("Tài liệu 2", doc2);

    // 2. Gửi duyệt 2 tài liệu (Draft -> Submitted)
    Console.WriteLine("\n[2] GỬI DUYỆT 2 TÀI LIỆU (POST /documents/{id}/submit)");
    Console.WriteLine("------------------------------------------------------------------------");

    doc1 = await TransitionAsync($"documents/{doc1.Id}/submit",
        new TransitionDocumentRequestDto("nguyen.kim.thanh.long", "Trình duyệt đề án.", doc1.Version));
    doc2 = await TransitionAsync($"documents/{doc2.Id}/submit",
        new TransitionDocumentRequestDto("nguyen.kim.thanh.long", "Trình duyệt dự toán.", doc2.Version));

    PrintDocumentSummary("Tài liệu 1 sau Submit", doc1);
    PrintDocumentSummary("Tài liệu 2 sau Submit", doc2);

    // 3. Duyệt tài liệu 1 (Submitted -> Approved)
    Console.WriteLine("\n[3] PHÊ DUYỆT TÀI LIỆU 1 (POST /documents/{id}/approve)");
    Console.WriteLine("------------------------------------------------------------------------");

    doc1 = await TransitionAsync($"documents/{doc1.Id}/approve",
        new TransitionDocumentRequestDto("truong.phong.hc", "Đồng ý phê duyệt triển khai ngay.", doc1.Version));
    PrintDocumentSummary("Tài liệu 1 sau Approve", doc1);

    // 4. Từ chối tài liệu 2 (Submitted -> Rejected)
    Console.WriteLine("\n[4] TỪ CHỐI TÀI LIỆU 2 (POST /documents/{id}/reject)");
    Console.WriteLine("------------------------------------------------------------------------");

    doc2 = await TransitionAsync($"documents/{doc2.Id}/reject",
        new TransitionDocumentRequestDto("ke.toan.truong", "Vượt hạn mức ngân sách quý, cần điều chỉnh lại.", doc2.Version));
    PrintDocumentSummary("Tài liệu 2 sau Reject", doc2);

    // 5. Giả lập 2 luồng đồng thời (Task.WhenAll) cùng xử lý 1 tài liệu Submitted
    Console.WriteLine("\n[5] GIẢ LẬP XUNG ĐỘT ĐỒNG THỜI (TASK.WHENALL - OPTIMISTIC CONCURRENCY)");
    Console.WriteLine("------------------------------------------------------------------------");

    var raceDoc = await CreateDocumentAsync(
        new CreateDocumentRequestDto(
            "Tài liệu kiểm thử Race Condition",
            "Kiểm tra 2 người phê duyệt gửi lệnh đồng thời vào cùng một phiên bản.",
            "nguyen.kim.thanh.long",
            "ban.giam.doc"),
        null);

    raceDoc = await TransitionAsync($"documents/{raceDoc.Id}/submit",
        new TransitionDocumentRequestDto("nguyen.kim.thanh.long", "Gửi duyệt để test xung đột đồng thời.", raceDoc.Version));

    Console.WriteLine($"-> Tài liệu test: {raceDoc.Id} | Status: {raceDoc.Status} | Version: v{raceDoc.Version}");
    Console.WriteLine("-> Đang bắn đồng thời 2 request Approve & Reject với cùng ExpectedVersion...");

    var taskA = SendTransitionRawAsync($"documents/{raceDoc.Id}/approve",
        new TransitionDocumentRequestDto("nguoi.duyet.A", "Luồng A: Phê duyệt đồng thời", raceDoc.Version));
    var taskB = SendTransitionRawAsync($"documents/{raceDoc.Id}/reject",
        new TransitionDocumentRequestDto("nguoi.duyet.B", "Luồng B: Từ chối đồng thời", raceDoc.Version));

    var results = await Task.WhenAll(taskA, taskB);
    PrintRaceResult("Luồng A (Approve)", results[0]);
    PrintRaceResult("Luồng B (Reject) ", results[1]);

    // 6. In toàn bộ History (Audit Trail) của các tài liệu
    Console.WriteLine("\n[6] TOÀN BỘ LỊCH SỬ XỬ LÝ (AUDIT TRAIL)");
    Console.WriteLine("------------------------------------------------------------------------");

    await PrintHistoryAsync(doc1.Id);
    await PrintHistoryAsync(doc2.Id);
    await PrintHistoryAsync(raceDoc.Id);

    Console.WriteLine("========================================================================");
    Console.WriteLine("  HOÀN THÀNH KỊCH BẢN KIỂM THỬ TỰ ĐỘNG.");
    Console.WriteLine("========================================================================");
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

async Task<DocumentDto> CreateDocumentAsync(CreateDocumentRequestDto request, string? idempotencyKey)
{
    using var msg = new HttpRequestMessage(HttpMethod.Post, "documents")
    {
        Content = JsonContent.Create(request, options: jsonOptions)
    };
    if (!string.IsNullOrWhiteSpace(idempotencyKey))
    {
        msg.Headers.Add("Idempotency-Key", $"{idempotencyKey}-{Guid.NewGuid():N}");
    }

    var response = await httpClient.SendAsync(msg);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<DocumentDto>(jsonOptions)
        ?? throw new InvalidOperationException("Không đọc được phản hồi tạo tài liệu.");
}

async Task<DocumentDto> TransitionAsync(string url, TransitionDocumentRequestDto request)
{
    var response = await httpClient.PostAsJsonAsync(url, request, jsonOptions);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<DocumentDto>(jsonOptions)
        ?? throw new InvalidOperationException("Không đọc được phản hồi chuyển trạng thái.");
}

async Task<(HttpStatusCode StatusCode, DocumentDto? Document, string? Error)> SendTransitionRawAsync(
    string url,
    TransitionDocumentRequestDto request)
{
    var response = await httpClient.PostAsJsonAsync(url, request, jsonOptions);
    if (response.IsSuccessStatusCode)
    {
        var dto = await response.Content.ReadFromJsonAsync<DocumentDto>(jsonOptions);
        return (response.StatusCode, dto, null);
    }

    var err = await response.Content.ReadFromJsonAsync<ApiErrorDto>(jsonOptions);
    return (response.StatusCode, null, err?.Error ?? response.ReasonPhrase);
}

async Task PrintHistoryAsync(Guid documentId)
{
    var detail = await httpClient.GetFromJsonAsync<DocumentDetailDto>($"documents/{documentId}", jsonOptions)
        ?? throw new InvalidOperationException("Không đọc được chi tiết tài liệu.");

    Console.WriteLine($"Tài liệu: {detail.Title}");
    Console.WriteLine($"  Id: {detail.Id} | Trạng thái cuối: {detail.Status} | Version: v{detail.Version}");
    foreach (var h in detail.History)
    {
        var transition = h.FromStatus.HasValue ? $"{h.FromStatus} -> {h.ToStatus}" : $"{h.ToStatus}";
        Console.WriteLine($"  * [{h.Timestamp:HH:mm:ss.fff}] {h.Action,-9} ({transition}) | By: {h.PerformedBy}");
        Console.WriteLine($"    Note: {h.Note} | HistoryId: {h.HistoryId}");
    }
    Console.WriteLine();
}

static void PrintDocumentSummary(string label, DocumentDto doc)
{
    Console.WriteLine($"-> {label}: [{doc.Status}] (v{doc.Version}) {doc.Title}");
    Console.WriteLine($"   Id: {doc.Id} | CreatedBy: {doc.CreatedBy} | Approver: {doc.Approver}");
}

static void PrintRaceResult(string label, (HttpStatusCode StatusCode, DocumentDto? Document, string? Error) result)
{
    if (result.Document is not null)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"   {label}: HTTP {(int)result.StatusCode} ({result.StatusCode}) -> Thành công! Status: {result.Document.Status}, Version: v{result.Document.Version}");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"   {label}: HTTP {(int)result.StatusCode} ({result.StatusCode}) -> Bị chặn: {result.Error}");
        Console.ResetColor();
    }
}
