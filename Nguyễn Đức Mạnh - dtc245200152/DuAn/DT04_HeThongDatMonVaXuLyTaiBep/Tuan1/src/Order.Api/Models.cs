// Mô hình dữ liệu nội bộ của API. Không trả trực tiếp ra ngoài, chỉ trả DTO.

public enum OrderStatus
{
    Pending,
    Preparing,
    Ready,
    Cancelled
}

public class MenuItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class OrderItem
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = "";
    public List<OrderItem> Items { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
}
