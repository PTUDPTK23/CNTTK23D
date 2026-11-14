
using System.Net;
using System.Net.Http.Json;

// 1. Cấu hình địa chỉ API
var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5100";

// 2. Tạo HttpClient
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// NẤC 1: Lấy danh sách sản phẩm
var products = await http.GetFromJsonAsync<List<ProductDto>>(
    "/api/products");

Console.WriteLine("Danh sach san pham:");

foreach (var p in products ?? [])
{
    Console.WriteLine($" [{p.Id}] {p.Name} - {p.Price:N0} VND");
}

// NẤC 2: Tạo sản phẩm mới
var createResponse = await http.PostAsJsonAsync(
    "/api/products",
    new CreateProductRequest(
        "Nhap mon REST API",
        "Tai lieu bai 5",
        80000
    )
);

var createdProduct =
    await createResponse.Content.ReadFromJsonAsync<ProductDto>();

Console.WriteLine($"\nDa tao san pham moi: " +
    $"[{createdProduct?.Id}] {createdProduct?.Name}");

Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");

// NẤC 3: Lấy chi tiết sản phẩm có ID = 1
var detail = await http.GetFromJsonAsync<ProductDto>(
    "/api/products/1");

Console.WriteLine($"\nChi tiet san pham 1: {detail?.Name}");

// NẤC 4: Cố ý gửi dữ liệu sai để kiểm tra lỗi 400
var invalidResponse = await http.PostAsJsonAsync(
    "/api/products",
    new CreateProductRequest("", "", -1)
);

Console.WriteLine(
    $"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode} " +
    $"{invalidResponse.StatusCode}"
);

var invalidError =
    await invalidResponse.Content.ReadFromJsonAsync<ApiError>();

Console.WriteLine(
    $"{invalidError?.Code}: {invalidError?.Message}"
);

// Các DTO phía Client
record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price
);

record CreateProductRequest(
    string Name,
    string Description,
    decimal Price
);

record ApiError(
    string Code,
    string Message
);
