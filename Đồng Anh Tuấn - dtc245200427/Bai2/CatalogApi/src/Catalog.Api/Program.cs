var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu lưu trong bộ nhớ tiến trình máy chủ.
var products = new List<ProductDto>
{
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

// GET /: kiểm tra máy chủ đang chạy.
app.MapGet("/", () => Results.Ok(new
{
    service = "Catalog.Api",
    status = "running"
}));

// GET /api/products: danh sách sản phẩm.
app.MapGet("/api/products", () => Results.Ok(products));

// GET /api/products/{id}: chi tiết một sản phẩm.
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null
        ? (IResult)Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham."))
        : Results.Ok(product);
});

// POST /api/products: tạo sản phẩm mới từ JSON.
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return (IResult)Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }

    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price);

    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

// DTO: dữ liệu sản phẩm trả về cho client.
record ProductDto(int Id, string Name, string Description, decimal Price);

// DTO: dữ liệu tạo sản phẩm do client gửi lên, không có Id.
record CreateProductRequest(string Name, string? Description, decimal Price);

// Lỗi API thống nhất.
record ApiError(string Code, string Message);
