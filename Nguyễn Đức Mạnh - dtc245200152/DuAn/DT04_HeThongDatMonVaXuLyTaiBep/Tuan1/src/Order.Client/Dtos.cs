// Client không tham chiếu code của API.
// Các record dưới đây chỉ cần cùng hình dạng JSON với DTO phía API.

public record MenuItemDto(int Id, string Name, decimal Price);

public record OrderItemDto(int MenuItemId, string Name, int Quantity, decimal UnitPrice);

// Trạng thái nhận về dạng chuỗi, ví dụ "Pending"
public record OrderDto(
    Guid Id,
    string CustomerName,
    List<OrderItemDto> Items,
    DateTimeOffset CreatedAt,
    string Status);

public record CreateOrderItemRequest(int MenuItemId, int Quantity);

public record CreateOrderRequest(string? CustomerName, List<CreateOrderItemRequest>? Items);

public record ApiError(string Code, string Message);
