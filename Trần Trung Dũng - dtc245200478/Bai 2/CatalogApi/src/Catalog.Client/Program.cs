using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Bước 1: GET danh sách sản phẩm
var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products")
    ?? new List<ProductDto>();
Console.WriteLine("Danh sach san pham:");
foreach (var product in products)
{
    Console.WriteLine($"  [{product.Id}] {product.Name} - {product.Price:N0} VND");
}

record ProductDto(int Id, string Name, string Description, decimal Price);
