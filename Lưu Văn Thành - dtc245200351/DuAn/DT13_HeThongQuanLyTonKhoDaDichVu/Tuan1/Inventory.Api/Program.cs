var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var inventories = new List<InventoryItem>
{
    new(1, "SP001", "Laptop Dell XPS 13", "Kho Ha Noi", 15),
    new(2, "SP002", "Chuot Logitech MX Master 3", "Kho TP.HCM", 50),
    new(3, "SP003", "Ban phim co Keychron K2", "Kho Da Nang", 30)
};

app.MapGet("/inventory", () => Results.Ok(inventories));

app.MapGet("/inventory/{id:int}", (int id) =>
{
    var item = inventories.FirstOrDefault(x => x.Id == id);
    return item is not null ? Results.Ok(item) : Results.NotFound(new { Message = "Khong tim thay ma ton kho." });
});

app.MapPost("/inventory", (CreateInventoryRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Sku) || request.Quantity < 0)
        return Results.BadRequest(new { Message = "Du lieu khong hop le." });

    int newId = inventories.Count > 0 ? inventories.Max(x => x.Id) + 1 : 1;
    var newItem = new InventoryItem(newId, request.Sku, request.ProductName, request.Warehouse, request.Quantity);
    inventories.Add(newItem);

    return Results.Created($"/inventory/{newItem.Id}", newItem);
});

app.MapPut("/inventory/{id:int}/adjust", (int id, AdjustStockRequest request) =>
{
    var index = inventories.FindIndex(x => x.Id == id);
    if (index == -1) return Results.NotFound(new { Message = "Khong tim thay san pham." });

    var current = inventories[index];
    int updatedQuantity = current.Quantity + request.QuantityChange;
    if (updatedQuantity < 0) return Results.BadRequest(new { Message = "Ton kho khong du de xuat." });

    var updatedItem = current with { Quantity = updatedQuantity };
    inventories[index] = updatedItem;
    return Results.Ok(updatedItem);
});

app.Run();

record InventoryItem(int Id, string Sku, string ProductName, string Warehouse, int Quantity);
record CreateInventoryRequest(string Sku, string ProductName, string Warehouse, int Quantity);
record AdjustStockRequest(int QuantityChange);
