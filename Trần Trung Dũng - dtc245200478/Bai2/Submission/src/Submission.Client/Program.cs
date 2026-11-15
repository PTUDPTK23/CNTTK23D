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
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"\nLoi: HTTP {(int)createResponse.StatusCode} - {error?.Code}: {error?.Message}");
    return;
}

// Bước 3: GET lại chi tiết một sản phẩm
var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");
Console.WriteLine($"\nChi tiet san pham 1: {detail?.Name ?? "khong tim thay"}");

// Bước 4: cố ý gửi dữ liệu sai để đọc lỗi 400
var invalidRequest = new CreateProductRequest("", "", -1m);
var invalidResponse = await http.PostAsJsonAsync("/api/products", invalidRequest);
Console.WriteLine($"\nThu gui du lieu sai: HTTP {(int)invalidResponse.StatusCode}");
var invalidError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"{invalidError?.Code}: {invalidError?.Message}");

record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
