var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu (Vùng 2)
var products = new List<ProductDto>
{
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

// Endpoint kiểm tra máy chủ (Vùng 3)
app.MapGet("/", () => Results.Ok(new {
    service = "Catalog.Api",
    status = "running"
}));

// Endpoint danh sách sản phẩm (Vùng 3)
app.MapGet("/api/products", () => Results.Ok(products));

// GET /api/products/{id:int} : chi tiết một sản phẩm (Vùng 3)
app.MapGet("/api/products/{id:int}", (int id) => {
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null
        ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham."))
        : Results.Ok(product);
});

// POST /api/products : tạo mới sản phẩm từ body JSON (Vùng 3)
app.MapPost("/api/products", (CreateProductRequest request) => {
    // Kiểm tra tính hợp lệ tại biên hệ thống
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0) {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."
        ));
    }

    // Máy chủ tự sinh Id, thêm vào danh sách bộ nhớ
    var newId = products.Max(p => p.Id) + 1;
    var product = new ProductDto(
        Id: newId,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price
    );

    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

// Khai báo kiểu / DTO ở cuối tệp (Vùng 5)
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);