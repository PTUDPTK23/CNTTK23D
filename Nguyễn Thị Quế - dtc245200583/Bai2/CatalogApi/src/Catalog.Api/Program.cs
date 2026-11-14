var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
// Dữ liệu mẫu trong bộ nhớ
var products = new List<ProductDto>
{
new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

app.MapGet("/", () => Results.Ok(new {
    service = "Catalog.Api",
    status = "running"
}));
// GET /api/products: lấy danh sách sản phẩm
app.MapGet("/api/products", () =>
{
    return Results.Ok(products);
});
// GET /api/products/{id:int} : chi tiết một sản phẩm
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    if (product == null)
    {
        return Results.NotFound(
            new ApiError(
                "PRODUCT_NOT_FOUND",
                $"Product with ID {id} was not found."
            )
        );
    }

    return Results.Ok(product);
});
// POST /api/products: thêm sản phẩm mới
app.MapPost("/api/products", (ProductCreateRequest request) =>
{
    // Kiểm tra dữ liệu đầu vào
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(
            new ApiError("INVALID_NAME", "Product name is required.")
        );
    }

    if (string.IsNullOrWhiteSpace(request.Description))
    {
        return Results.BadRequest(
            new ApiError("INVALID_DESCRIPTION", "Product description is required.")
        );
    }

    if (request.Price <= 0)
    {
        return Results.BadRequest(
            new ApiError("INVALID_PRICE", "Product price must be greater than 0.")
        );
    }

    // Tạo ID mới
    int newId = products.Max(p => p.Id) + 1;

    // Tạo sản phẩm
    var newProduct = new ProductDto(
        newId,
        request.Name,
        request.Description,
        request.Price
    );

    // Thêm vào danh sách
    products.Add(newProduct);

    // Trả về HTTP 201 Created
    return Results.Created(
        $"/api/products/{newId}",
        newProduct
    );
});
app.Run();
// DTO: hình dạng dữ liệu trao đổi qua mạng
record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price
);
record ApiError(string Code,string Message);
record ProductCreateRequest(
    string Name,
    string Description,
    decimal Price
);