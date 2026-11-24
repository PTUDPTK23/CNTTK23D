
using System;
using System.Linq;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// DỮ LIỆU MẪU
var products = new List<ProductDto>
{
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

// GET /: Kiểm tra máy chủ đang chạy
app.MapGet("/", () => Results.Ok(new
{
    service = "Catalog.Api",
    status = "running"
}));

// GET /api/products: Danh sách sản phẩm
app.MapGet("/api/products", () => Results.Ok(products));

// GET /api/products/{id:int}: Chi tiết sản phẩm
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    return product is null
        ? Results.NotFound(new ApiError(
            "PRODUCT_NOT_FOUND",
            "Khong tim thay san pham."
        ))
        : Results.Ok(product);
});

// POST /api/products: Tạo sản phẩm mới
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    // Kiểm tra dữ liệu đầu vào
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."
        ));
    }

    // Máy chủ tự sinh Id và tạo sản phẩm
    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price
    );

    // Lưu sản phẩm vào danh sách
    products.Add(product);

    // Trả về HTTP 201 Created kèm Location
    return Results.Created($"/api/products/{product.Id}", product);
});

// Khởi động máy chủ
app.Run();

// DTO: Hình dạng dữ liệu đi qua mạng
record ProductDto(int Id, string Name, string Description, decimal Price);

record CreateProductRequest(string Name, string? Description, decimal Price);

record ApiError(string Code, string Message);
