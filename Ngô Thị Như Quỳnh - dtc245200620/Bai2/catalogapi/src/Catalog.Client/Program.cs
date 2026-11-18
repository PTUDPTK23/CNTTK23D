
using System.Net;
using System.Net.Http.Json;

// Dia chi may chu API
var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5100";

// Tao HttpClient
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// BUOC 7: Lay danh sach san pham
var products = await http.GetFromJsonAsync<List<ProductDto>>(
    "/api/products");

Console.WriteLine("Danh sach san pham:");

foreach (var p in products ?? [])
{
    Console.WriteLine(
        $"[{p.Id}] {p.Name} - {p.Price:N0} VND");
}

// BUOC 8.1 - 8.2: Gui POST tao san pham
var createRequest = new CreateProductRequest(
    "Nhap mon REST API",
    "Sach tham khao",
    120000);

var createResponse = await http.PostAsJsonAsync(
    "/api/products", createRequest);

if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content
        .ReadFromJsonAsync<ProductDto>();

    Console.WriteLine(
        $"\nDa tao san pham moi: [{created?.Id}] {created?.Name}");

    Console.WriteLine(
        $"Doc lai tai: {createResponse.Headers.Location}");
}
else
{
    var error = await createResponse.Content
        .ReadFromJsonAsync<ApiError>();

    Console.WriteLine(
        $"\nLoi: {error?.Code} - {error?.Message}");

    return;
}

// BUOC 8.3: Doc chi tiet san pham
var detail = await http.GetFromJsonAsync<ProductDto>(
    "/api/products/1");

Console.WriteLine(
    $"\nChi tiet san pham 1: {detail?.Name}");

// BUOC 8.4: Gui du lieu sai de kiem tra loi 400
var invalidResponse = await http.PostAsJsonAsync(
    "/api/products",
    new CreateProductRequest("", "", -1));

Console.WriteLine(
    $"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");

var invalidError = await invalidResponse.Content
    .ReadFromJsonAsync<ApiError>();

Console.WriteLine(
    $"{invalidError?.Code}: {invalidError?.Message}");

// KHAI BAO DTO
record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price);

record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price);

record ApiError(
    string Code,
    string Message);
