using System.Net;
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

// Bước 2: POST sản phẩm hợp lệ; kiểm tra status trước khi đọc body
var createRequest = new CreateProductRequest("Lap trinh gRPC", "Tai lieu bai 5", 80000m);
var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);
if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
    if (created is null)
    {
        Console.WriteLine("Body Created rong.");
        return;
    }

    Console.WriteLine($"\nDa tao san pham moi: [{created.Id}] {created.Name}");
    Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");
}
else
{
    Console.WriteLine($"\nLoi: HTTP {(int)createResponse.StatusCode}");
    return;
}

record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
