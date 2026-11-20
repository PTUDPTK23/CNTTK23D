var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var products = new List<Product>
{
    new(1, "Giao trinh Lap trinh phan tan", "Tai lieu mon hoc", 150000),
    new(2, "Cam nang Thiet ke RESTful API (Ban moi)", "Tai lieu tham khao", 115000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap co loi giai", 120000)
};

app.MapGet("/api/products", () => Results.Ok(products));

app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null
        ? Results.NotFound(new { code = "NOT_FOUND", message = $"Khong tim thay san pham co id = {id}" })
        : Results.Ok(product);
});

app.MapPost("/api/products", (CreateProductRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(new
        {
            code = "INVALID_PRODUCT",
            message = "Ten san pham khong duoc de trong va gia phai lon hon 0."
        });
    }

    var nextId = products.Count == 0 ? 1 : products.Max(p => p.Id) + 1;
    var created = new Product(nextId, request.Name.Trim(), request.Description ?? "", request.Price);
    products.Add(created);
    return Results.Created($"/api/products/{created.Id}", created);
});

app.Run();

record Product(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string Description, decimal Price);