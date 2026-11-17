var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var products = new List<Product>
{
    new Product { Id = 1, Name = "Laptop", Price = 15000000 },
    new Product { Id = 2, Name = "Chuot", Price = 500000 },
    new Product { Id = 3, Name = "Ban phim", Price = 1000000 }
};

// GET: Lay tat ca san pham
app.MapGet("/api/products", () =>
{
    return Results.Ok(products);
});

// GET: Lay san pham theo ID
app.MapGet("/api/products/{id}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    if (product == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(product);
});

// POST: Them san pham moi
app.MapPost("/api/products", (Product product) =>
{
    product.Id = products.Count > 0
        ? products.Max(p => p.Id) + 1
        : 1;

    products.Add(product);

    return Results.Created($"/api/products/{product.Id}", product);
});

// PUT: Sua san pham theo ID
app.MapPut("/api/products/{id}", (int id, Product updatedProduct) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    if (product == null)
    {
        return Results.NotFound();
    }

    product.Name = updatedProduct.Name;
    product.Price = updatedProduct.Price;

    return Results.Ok(product);
});

// DELETE: Xoa san pham theo ID
app.MapDelete("/api/products/{id}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    if (product == null)
    {
        return Results.NotFound();
    }

    products.Remove(product);

    return Results.NoContent();
});

app.Run();

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public decimal Price { get; set; }
}