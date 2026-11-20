// Lưu dữ liệu trong bộ nhớ bằng List<T>. Đăng ký Singleton nên dùng chung cho mọi request.
// Dữ liệu sẽ mất khi dừng API (cơ sở dữ liệu thêm ở tuần sau).
public class CampaignStore
{
    private readonly List<Contact> _contacts = Enumerable.Range(1, 5)
        .Select(i => new Contact
        {
            Id = i,
            Name = $"Người nhận {i:D2}",
            Email = $"nguoinhan{i:D2}@example.com",
            Phone = $"+849000000{i:D2}"
        })
        .ToList();

    private readonly List<Campaign> _campaigns = [];

    // Nhiều request có thể chạy cùng lúc nên khóa khi đọc/ghi danh sách chiến dịch
    private readonly object _lock = new();

    public List<Contact> GetContacts() => _contacts;

    public Contact? FindContact(int id) => _contacts.FirstOrDefault(c => c.Id == id);

    public void AddCampaign(Campaign campaign)
    {
        lock (_lock)
        {
            _campaigns.Add(campaign);
        }
    }

    public Campaign? FindCampaign(Guid id)
    {
        lock (_lock)
        {
            return _campaigns.FirstOrDefault(c => c.Id == id);
        }
    }

    public List<Campaign> GetCampaigns(NotificationChannel? channel)
    {
        lock (_lock)
        {
            return _campaigns
                .Where(c => channel is null || c.Channel == channel)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
        }
    }
}
