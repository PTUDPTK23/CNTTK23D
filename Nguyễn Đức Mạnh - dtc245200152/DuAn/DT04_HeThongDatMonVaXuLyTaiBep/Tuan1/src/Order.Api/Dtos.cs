// DTO là hợp đồng dữ liệu tại biên API (Bài 2).

// Dữ liệu trả về cho client
public record MenuItemDto(int Id, string Name, decimal Price);

public record OrderItemDto(int MenuItemId, string Name, int Quantity, decimal UnitPrice);

public record OrderDto(
    Guid Id,
    string CustomerName,
    List<OrderItemDto> Items,
    DateTimeOffset CreatedAt,
    OrderStatus Status);

// Dữ liệu client được phép gửi lên: không có Id, thời gian, trạng thái (server tự sinh)
public record CreateOrderItemRequest(int MenuItemId, int Quantity);

public record CreateOrderRequest(string? CustomerName, List<CreateOrderItemRequest>? Items);

// Cấu trúc lỗi thống nhất cho mọi phản hồi lỗi
public record ApiError(string Code, string Message);
