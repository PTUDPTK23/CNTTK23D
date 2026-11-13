var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu (vùng 2, ngay sau khi tạo app)
var products = new List<ProductDto>
{
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000m),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000m),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000m)
};

// GET /: kiểm tra dịch vụ đang chạy
app.MapGet("/", () => Results.Ok(new
{
    service = "Catalog.Api",
    status = "running"
}));

// GET /api/products: danh sách sản phẩm
app.MapGet("/api/products", () => Results.Ok(products));

// GET /api/products/{id}: chi tiết một sản phẩm
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null
        ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm."))
        : Results.Ok(product);
});

// POST /api/products: tạo sản phẩm mới từ body JSON
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Tên sản phẩm không được trống và giá phải lớn hơn 0."));
    }

    var product = new ProductDto(
        products.Max(p => p.Id) + 1,
        request.Name.Trim(),
        request.Description?.Trim() ?? "",
        request.Price);

    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

// DTO đầu ra, DTO đầu vào và lỗi API (vùng 5, sau app.Run())
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
