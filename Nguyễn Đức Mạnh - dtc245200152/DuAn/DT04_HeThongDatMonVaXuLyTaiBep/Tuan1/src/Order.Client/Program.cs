using System.Net;
using System.Net.Http.Json;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

// Địa chỉ API lấy từ tham số dòng lệnh
var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine($"Gọi Order.Api tại {baseUrl}");

try
{
    // 1. Xem menu
    Console.WriteLine("\n=== 1. Xem menu ===");
    var menu = await http.GetFromJsonAsync<List<MenuItemDto>>("/menu") ?? [];
    foreach (var m in menu)
        Console.WriteLine($"  [{m.Id}] {m.Name} - {m.Price:N0} đ");

    // 2. Đặt một đơn hợp lệ
    Console.WriteLine("\n=== 2. Đặt món ===");
    var newOrder = new CreateOrderRequest("Nguyễn Văn A",
    [
        new CreateOrderItemRequest(1, 2),
        new CreateOrderItemRequest(4, 1)
    ]);
    var createResponse = await http.PostAsJsonAsync("/orders", newOrder);
    Console.WriteLine($"  Status: {(int)createResponse.StatusCode} {createResponse.StatusCode}");

    // Kiểm tra status code trước khi đọc body
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintError(createResponse);
        return;
    }
    var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
    Console.WriteLine($"  Location: {createResponse.Headers.Location}");
    PrintOrder(created!);

    // 3. Đọc lại đơn vừa tạo
    Console.WriteLine("\n=== 3. Đọc lại đơn vừa tạo ===");
    await GetOrder(created!.Id);

    // 4. Lọc đơn theo tên khách (query string)
    Console.WriteLine("\n=== 4. Lọc đơn theo khách ===");
    var filterResponse = await http.GetAsync($"/orders?customer={Uri.EscapeDataString("Nguyễn Văn A")}");
    if (filterResponse.IsSuccessStatusCode)
    {
        var orders = await filterResponse.Content.ReadFromJsonAsync<List<OrderDto>>() ?? [];
        Console.WriteLine($"  Tìm thấy {orders.Count} đơn");
        foreach (var o in orders)
            PrintOrder(o);
    }
    else
    {
        await PrintError(filterResponse);
    }

    // 5. Cố ý gửi đơn sai để nhận 400
    Console.WriteLine("\n=== 5. Gửi đơn sai (mong đợi 400) ===");
    var badOrder = new CreateOrderRequest("", [new CreateOrderItemRequest(1, 0)]);
    var badResponse = await http.PostAsJsonAsync("/orders", badOrder);
    if (badResponse.IsSuccessStatusCode)
        Console.WriteLine("  Không mong đợi: API chấp nhận đơn sai.");
    else
        await PrintError(badResponse);

    // 6. Tra một id không tồn tại để nhận 404
    Console.WriteLine("\n=== 6. Tra đơn không tồn tại (mong đợi 404) ===");
    await GetOrder(Guid.NewGuid());
}
catch (HttpRequestException ex)
{
    // Lỗi kết nối: API chưa chạy hoặc sai địa chỉ (khác với lỗi HTTP 400/404)
    Console.WriteLine($"\nLỗi kết nối tới {baseUrl}: {ex.Message}");
    Console.WriteLine("Kiểm tra API đã chạy chưa và địa chỉ có đúng không.");
}

async Task GetOrder(Guid id)
{
    var response = await http.GetAsync($"/orders/{id}");
    Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
    if (response.StatusCode == HttpStatusCode.OK)
    {
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        PrintOrder(order!);
    }
    else
    {
        await PrintError(response);
    }
}

static async Task PrintError(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"  Lỗi {(int)response.StatusCode} - {error?.Code}: {error?.Message}");
}

static void PrintOrder(OrderDto order)
{
    Console.WriteLine($"  Đơn {order.Id} | Khách: {order.CustomerName} | Trạng thái: {order.Status} | Lúc: {order.CreatedAt:HH:mm:ss}");
    foreach (var item in order.Items)
        Console.WriteLine($"    - {item.Name} x{item.Quantity} ({item.UnitPrice:N0} đ)");
}
