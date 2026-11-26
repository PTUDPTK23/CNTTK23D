using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;

// Địa chỉ API lấy từ tham số dòng lệnh
var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine($"Gọi LibraryApi tại {baseUrl}");

try
{
    // 1. Xem danh sách sách và chi tiết một cuốn sách
    Console.WriteLine("\n=== 1. Xem danh sách sách ===");
    var books = await http.GetFromJsonAsync<List<BookDto>>("/books") ?? [];
    foreach (var b in books)
        Console.WriteLine($"  [{b.Id}] {b.Title} - {b.Author} (Khả dụng: {b.AvailableCopies}/{b.TotalCopies})");

    if (books.Count > 0)
    {
        var detail = await http.GetFromJsonAsync<BookDto>($"/books/{books[0].Id}");
        if (detail is not null)
            Console.WriteLine($"  Chi tiết sách [{detail.Id}]: {detail.Title} | ISBN: {detail.ISBN} | {detail.Description}");
    }

    // 2. Tạo yêu cầu đặt mượn hợp lệ (userId = 1, bookId = 1)
    Console.WriteLine("\n=== 2. Tạo yêu cầu đặt mượn ===");
    var newReservation = new CreateReservationRequest(1, 1);
    var createResponse = await http.PostAsJsonAsync("/reservations", newReservation);
    Console.WriteLine($"  Status: {(int)createResponse.StatusCode} {createResponse.StatusCode}");

    // Kiểm tra status code trước khi đọc body
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintError(createResponse);
        return;
    }
    var created = await createResponse.Content.ReadFromJsonAsync<ReservationDto>();
    Console.WriteLine($"  Location: {createResponse.Headers.Location}");
    PrintReservation(created!);

    // 3. Đọc lại yêu cầu đặt mượn theo Location
    Console.WriteLine("\n=== 3. Đọc lại yêu cầu đặt mượn vừa tạo ===");
    await GetReservation(createResponse.Headers.Location!.ToString());

    // 4. Xem danh sách đặt mượn của người dùng
    Console.WriteLine("\n=== 4. Xem danh sách đặt mượn của người dùng 1 ===");
    var userResResponse = await http.GetAsync("/users/1/reservations");
    if (userResResponse.IsSuccessStatusCode)
    {
        var reservations = await userResResponse.Content.ReadFromJsonAsync<List<ReservationDto>>() ?? [];
        Console.WriteLine($"  Tìm thấy {reservations.Count} yêu cầu đặt mượn");
        foreach (var r in reservations)
            Console.WriteLine($"  - #{r.Id} | {r.BookTitle} | Trạng thái: {r.Status} | Lúc: {r.CreatedAt:HH:mm:ss}");
    }
    else
    {
        await PrintError(userResResponse);
    }

    // 5. Cố ý gửi yêu cầu đặt mượn sai để nhận 400
    Console.WriteLine("\n=== 5. Gửi yêu cầu đặt mượn sai (mong đợi 400) ===");
    var badReservation = new CreateReservationRequest(0, 99);
    var badResponse = await http.PostAsJsonAsync("/reservations", badReservation);
    Console.WriteLine($"  Status: {(int)badResponse.StatusCode} {badResponse.StatusCode}");
    if (badResponse.IsSuccessStatusCode)
        Console.WriteLine("  Không mong đợi: API chấp nhận yêu cầu sai.");
    else
        await PrintError(badResponse);

    // 6. Tra một id không tồn tại để nhận 404
    Console.WriteLine("\n=== 6. Tra yêu cầu không tồn tại (mong đợi 404) ===");
    await GetReservation("/reservations/999999");

    // 7. Gọi sai route: framework trả 404 không có body, client vẫn không bị lỗi
    Console.WriteLine("\n=== 7. Gọi sai route (mong đợi 404 không có ApiError) ===");
    await GetReservation("/reservations/abc");
}
catch (HttpRequestException ex)
{
    // Lỗi kết nối: API chưa chạy hoặc sai địa chỉ (khác với lỗi HTTP 400/404)
    Console.WriteLine($"\nLỗi kết nối tới {baseUrl}: {ex.Message}");
    Console.WriteLine("Kiểm tra API đã chạy chưa và địa chỉ có đúng không.");
}

async Task GetReservation(string uri)
{
    var response = await http.GetAsync(uri);
    Console.WriteLine($"  GET {uri} → {(int)response.StatusCode} {response.StatusCode}");
    if (response.StatusCode == HttpStatusCode.OK)
    {
        var reservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        PrintReservation(reservation!);
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

    if (error is null || (string.IsNullOrWhiteSpace(error.Code) && string.IsNullOrWhiteSpace(error.Message)))
    {
        Console.WriteLine($"  Lỗi {(int)response.StatusCode} (không có ApiError trong phản hồi)");
        return;
    }

    var codeText = string.IsNullOrWhiteSpace(error.Code) ? "" : $" - {error.Code}";
    Console.WriteLine($"  Lỗi {(int)response.StatusCode}{codeText}: {error.Message}");
    foreach (var (field, messages) in error.Errors ?? [])
        Console.WriteLine($"    {field}: {string.Join("; ", messages)}");
}

static void PrintReservation(ReservationDto reservation)
{
    Console.WriteLine(
        $"  Yêu cầu #{reservation.Id} | Sách: {reservation.BookTitle} | Người dùng: {reservation.UserName} | Trạng thái: {reservation.Status} | Lúc: {reservation.CreatedAt:HH:mm:ss}");
}

// ============ DTO RECORDS (Client tự khai báo, không dùng chung project với API) ============

record BookDto(int Id, string Title, string Author, string ISBN, string? Description, int TotalCopies, int AvailableCopies);
record CreateReservationRequest(int UserId, int BookId);
record ReservationDto(int Id, int UserId, string? UserName, int BookId, string? BookTitle, string Status, DateTime CreatedAt, DateTime? UpdatedAt);
record ApiError(string? Code, string? Message, Dictionary<string, string[]>? Errors);
