
using System.Net;
using System.Net.Http.Json;

// 1. Địa chỉ máy chủ API
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";

// 2. Tạo HttpClient để giao tiếp với API
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// 3. Lấy danh sách sản phẩm từ API
var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");

// 4. Hiển thị danh sách sản phẩm
Console.WriteLine("Danh sach san pham:");

foreach (var p in products ?? [])
{
    Console.WriteLine($" [{p.Id}] {p.Name} - {p.Price:N0} VND");
}

// DTO phía Client: phải khớp cấu trúc JSON từ API
record ProductDto(int Id, string Name, string Description, decimal Price);
