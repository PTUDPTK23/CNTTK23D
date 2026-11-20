// Lưu dữ liệu trong bộ nhớ bằng List<T>. Đăng ký Singleton nên dùng chung cho mọi request.
// Dữ liệu sẽ mất khi dừng API.
public class OrderStore
{
    private readonly List<MenuItem> _menu =
    [
        new MenuItem { Id = 1, Name = "Phở bò", Price = 45000 },
        new MenuItem { Id = 2, Name = "Bún chả", Price = 40000 },
        new MenuItem { Id = 3, Name = "Cơm tấm", Price = 35000 },
        new MenuItem { Id = 4, Name = "Trà đá", Price = 5000 }
    ];

    private readonly List<Order> _orders = [];

    // Nhiều request có thể chạy cùng lúc nên khóa khi đọc/ghi danh sách đơn
    private readonly object _lock = new();

    public List<MenuItem> GetMenu() => _menu;

    public MenuItem? FindMenuItem(int id) => _menu.FirstOrDefault(m => m.Id == id);

    public void AddOrder(Order order)
    {
        lock (_lock)
        {
            _orders.Add(order);
        }
    }

    public Order? FindOrder(Guid id)
    {
        lock (_lock)
        {
            return _orders.FirstOrDefault(o => o.Id == id);
        }
    }

    public List<Order> GetOrders(string? customer, OrderStatus? status)
    {
        lock (_lock)
        {
            return _orders
                .Where(o => customer is null
                            || string.Equals(o.CustomerName, customer, StringComparison.OrdinalIgnoreCase))
                .Where(o => status is null || o.Status == status)
                .ToList();
        }
    }
}
