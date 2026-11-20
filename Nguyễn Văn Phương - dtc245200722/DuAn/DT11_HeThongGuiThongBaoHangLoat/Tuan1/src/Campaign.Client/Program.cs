using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;

// Địa chỉ API lấy từ tham số dòng lệnh
var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine($"Gọi Campaign.Api tại {baseUrl}");

try
{
    // 1. Xem danh bạ
    Console.WriteLine("\n=== 1. Xem danh bạ ===");
    var contacts = await http.GetFromJsonAsync<List<ContactDto>>("/contacts") ?? [];
    foreach (var c in contacts)
        Console.WriteLine($"  [{c.Id}] {c.Name} - {c.Email} - {c.Phone}");

    // 2. Tạo chiến dịch hợp lệ: chọn 2 người trong danh bạ và nhập thêm 1 người
    Console.WriteLine("\n=== 2. Tạo chiến dịch ===");
    var newCampaign = new CreateCampaignRequest(
        "Thông báo lịch thi",
        "Email",
        "Lịch thi dành cho {{name}}",
        "Chào {{name}}, lịch thi môn Phát triển ứng dụng phân tán: 8h00 ngày 25/11.",
        [1, 2],
        [new RecipientInput("Nguyễn Văn An", "an@example.com")]);
    var createResponse = await http.PostAsJsonAsync("/campaigns", newCampaign);
    Console.WriteLine($"  Status: {(int)createResponse.StatusCode} {createResponse.StatusCode}");

    // Kiểm tra status code trước khi đọc body
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintError(createResponse);
        return;
    }
    var created = await createResponse.Content.ReadFromJsonAsync<CampaignDto>();
    Console.WriteLine($"  Location: {createResponse.Headers.Location}");
    PrintCampaign(created!);

    // 3. Đọc lại chiến dịch theo Location
    Console.WriteLine("\n=== 3. Đọc lại chiến dịch vừa tạo ===");
    await GetCampaign(createResponse.Headers.Location!.ToString());

    // 4. Lọc chiến dịch theo kênh (query string)
    Console.WriteLine("\n=== 4. Lọc chiến dịch theo kênh Email ===");
    var filterResponse = await http.GetAsync("/campaigns?channel=Email");
    if (filterResponse.IsSuccessStatusCode)
    {
        var campaigns = await filterResponse.Content.ReadFromJsonAsync<List<CampaignSummaryDto>>() ?? [];
        Console.WriteLine($"  Tìm thấy {campaigns.Count} chiến dịch");
        foreach (var c in campaigns)
            Console.WriteLine($"  - {c.Name} | {c.Channel} | {c.RecipientCount} người nhận | {c.Status}");
    }
    else
    {
        await PrintError(filterResponse);
    }

    // 5. Cố ý gửi chiến dịch sai để nhận 400
    Console.WriteLine("\n=== 5. Gửi chiến dịch sai (mong đợi 400) ===");
    var badCampaign = new CreateCampaignRequest("", "Email", "", "", [99], [new RecipientInput(null, "khong-phai-email")]);
    var badResponse = await http.PostAsJsonAsync("/campaigns", badCampaign);
    Console.WriteLine($"  Status: {(int)badResponse.StatusCode} {badResponse.StatusCode}");
    if (badResponse.IsSuccessStatusCode)
        Console.WriteLine("  Không mong đợi: API chấp nhận chiến dịch sai.");
    else
        await PrintError(badResponse);

    // 6. Tra một id không tồn tại để nhận 404
    Console.WriteLine("\n=== 6. Tra chiến dịch không tồn tại (mong đợi 404) ===");
    await GetCampaign($"/campaigns/{Guid.NewGuid()}");

    // 7. Gọi sai route: framework trả 404 không có body, client vẫn không bị lỗi
    Console.WriteLine("\n=== 7. Gọi sai route (mong đợi 404 không có ApiError) ===");
    await GetCampaign("/campaigns/abc");
}
catch (HttpRequestException ex)
{
    // Lỗi kết nối: API chưa chạy hoặc sai địa chỉ (khác với lỗi HTTP 400/404)
    Console.WriteLine($"\nLỗi kết nối tới {baseUrl}: {ex.Message}");
    Console.WriteLine("Kiểm tra API đã chạy chưa và địa chỉ có đúng không.");
}

async Task GetCampaign(string uri)
{
    var response = await http.GetAsync(uri);
    Console.WriteLine($"  GET {uri} → {(int)response.StatusCode} {response.StatusCode}");
    if (response.StatusCode == HttpStatusCode.OK)
    {
        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>();
        PrintCampaign(campaign!);
    }
    else
    {
        await PrintError(response);
    }
}

// Nhánh lỗi đọc ApiError. Lỗi do framework chặn (sai route, JSON hỏng, thiếu Content-Type) có thể
// không có body, nên không giả định lúc nào cũng đọc được ApiError.
static async Task PrintError(HttpResponseMessage response)
{
    ApiError? error = null;
    try
    {
        if (response.Content.Headers.ContentLength is not 0)
            error = await response.Content.ReadFromJsonAsync<ApiError>();
    }
    catch (JsonException)
    {
    }

    if (error is null)
    {
        Console.WriteLine($"  Lỗi {(int)response.StatusCode} (không có ApiError trong phản hồi)");
        return;
    }

    Console.WriteLine($"  Lỗi {(int)response.StatusCode} - {error.Code}: {error.Message}");
    foreach (var (field, messages) in error.Errors ?? [])
        Console.WriteLine($"    {field}: {string.Join("; ", messages)}");
}

static void PrintCampaign(CampaignDto campaign)
{
    Console.WriteLine($"  Chiến dịch {campaign.Id} | {campaign.Name} | Kênh: {campaign.Channel} | Trạng thái: {campaign.Status} | Lúc: {campaign.CreatedAt:HH:mm:ss}");
    foreach (var r in campaign.Recipients)
        Console.WriteLine($"    - {r.Name} <{r.Address}>");
}
