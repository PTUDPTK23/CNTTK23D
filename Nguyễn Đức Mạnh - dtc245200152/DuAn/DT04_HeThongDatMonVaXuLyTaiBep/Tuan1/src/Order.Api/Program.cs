using System.Text.Json.Serialization;

// ===== Pha 1: chuẩn bị =====
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OrderStore>();

// Trả enum trạng thái dạng chuỗi ("Pending") thay vì số
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// ===== Pha 2: map endpoint và chạy =====

// Kiểm tra API đang chạy
app.MapGet("/", () => Results.Ok("Order.Api đang chạy"));

// Danh sách món
app.MapGet("/menu", (OrderStore store) =>
    Results.Ok(store.GetMenu().Select(ToMenuItemDto)));

// Tạo đơn: dữ liệu lấy từ body
app.MapPost("/orders", (CreateOrderRequest request, OrderStore store) =>
{
    // Kiểm tra dữ liệu tại biên, trước khi vào nghiệp vụ
    if (string.IsNullOrWhiteSpace(request.CustomerName))
        return Results.BadRequest(new ApiError("INVALID_ORDER", "Thiếu tên khách hàng."));

    if (request.Items is null || request.Items.Count == 0)
        return Results.BadRequest(new ApiError("INVALID_ORDER", "Đơn phải có ít nhất một món."));

    var items = new List<OrderItem>();
    foreach (var item in request.Items)
    {
        if (item.Quantity <= 0)
            return Results.BadRequest(new ApiError("INVALID_ORDER",
                $"Số lượng của món {item.MenuItemId} phải lớn hơn 0."));

        var menuItem = store.FindMenuItem(item.MenuItemId);
        if (menuItem is null)
            return Results.BadRequest(new ApiError("MENU_ITEM_NOT_FOUND",
                $"Không có món {item.MenuItemId} trong menu."));

        items.Add(new OrderItem
        {
            MenuItemId = menuItem.Id,
            Name = menuItem.Name,
            Quantity = item.Quantity,
            UnitPrice = menuItem.Price
        });
    }

    // Server tự sinh Id, thời gian và trạng thái ban đầu
    var order = new Order
    {
        Id = Guid.NewGuid(),
        CustomerName = request.CustomerName.Trim(),
        Items = items,
        CreatedAt = DateTimeOffset.UtcNow,
        Status = OrderStatus.Pending
    };
    store.AddOrder(order);

    // 201 Created + header Location + DTO trong body
    return Results.Created($"/orders/{order.Id}", ToOrderDto(order));
});

// Chi tiết đơn: định danh nằm trong path
app.MapGet("/orders/{id:guid}", (Guid id, OrderStore store) =>
{
    var order = store.FindOrder(id);
    return order is null
        ? Results.NotFound(new ApiError("ORDER_NOT_FOUND", $"Không tìm thấy đơn {id}."))
        : Results.Ok(ToOrderDto(order));
});

// Danh sách đơn: điều kiện lọc nằm trong query string
app.MapGet("/orders", (string? customer, string? status, OrderStore store) =>
{
    OrderStatus? statusFilter = null;
    if (status is not null)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var parsed) || !Enum.IsDefined(parsed))
            return Results.BadRequest(new ApiError("INVALID_STATUS",
                $"Trạng thái '{status}' không hợp lệ. Dùng: Pending, Preparing, Ready, Cancelled."));
        statusFilter = parsed;
    }

    var orders = store.GetOrders(customer, statusFilter);
    return Results.Ok(orders.Select(ToOrderDto));
});

app.Run();

// ===== Chuyển model nội bộ sang DTO =====
static MenuItemDto ToMenuItemDto(MenuItem m) => new(m.Id, m.Name, m.Price);

static OrderDto ToOrderDto(Order o) => new(
    o.Id,
    o.CustomerName,
    o.Items.Select(i => new OrderItemDto(i.MenuItemId, i.Name, i.Quantity, i.UnitPrice)).ToList(),
    o.CreatedAt,
    o.Status);
