// Mô hình dữ liệu nội bộ của API. Không trả trực tiếp ra ngoài, chỉ trả DTO.

public enum NotificationChannel
{
    Email,
    Sms
}

// Tuần 1 mới có Draft (vừa tạo). Các trạng thái gửi thêm ở tuần sau khi có start và hàng đợi.
public enum CampaignStatus
{
    Draft
}

public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
}

public class Recipient
{
    public int? ContactId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
}

public class Campaign
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
    public List<Recipient> Recipients { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public CampaignStatus Status { get; set; }
}
