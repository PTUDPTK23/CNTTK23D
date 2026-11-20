using System.Net;
using System.Net.Http.Json;

// Lấy địa chỉ API từ tham số dòng lệnh, mặc định là cổng 5255 của mày
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5255";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// ==========================================
// NẤC 1: Lấy danh sách sản phẩm (GET)
// ==========================================
var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");
Console.WriteLine("Danh sach san pham:");
foreach (var p in products ?? [])
{
    Console.WriteLine($"[{p.Id}] {p.Name} - {p.Price:N0} VND");
}
Console.WriteLine("----------------------------------");

// ==========================================
// NẤC 2: Gửi POST tạo sản phẩm mới và kiểm tra mã trạng thái TRƯỚC khi đọc body
// ==========================================
var createRequest = new CreateProductRequest("Nhap mon REST API", "Sach tham khao", 120000);
var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);

if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"Da tao san pham moi: [{created?.Id}] {created?.Name}");
    Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");
}
else
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"\nLoi: {error?.Code} - {error?.Message}");
    return;
}
Console.WriteLine("----------------------------------");

// ==========================================
// NẤC 3: Đọc lại chi tiết một sản phẩm (GET theo ID)
// ==========================================
var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");
Console.WriteLine($"Chi tiet san pham 1: {detail?.Name}");
Console.WriteLine("----------------------------------");

// ==========================================
// NẤC 4: Cố ý gửi dữ liệu sai để quan sát lỗi 400 Bad Request
// ==========================================
var invalidResponse = await http.PostAsJsonAsync("/api/products", new CreateProductRequest("", "", -1));

Console.WriteLine($"Thu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
var invalidError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"{invalidError?.Code}: {invalidError?.Message}");

// ==========================================
// Các record DTO (Contract chung với server)
// ==========================================
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);