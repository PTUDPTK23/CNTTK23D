// DTO là hợp đồng dữ liệu tại biên API (Bài 2).

// Dữ liệu trả về cho client
public record ContactDto(int Id, string Name, string Email, string Phone);

public record RecipientDto(string Name, string Address);

public record CampaignDto(
    Guid Id,
    string Name,
    NotificationChannel Channel,
    string Subject,
    string Content,
    List<RecipientDto> Recipients,
    DateTimeOffset CreatedAt,
    CampaignStatus Status);

// Bản rút gọn cho danh sách: không kèm nội dung và danh sách người nhận
public record CampaignSummaryDto(
    Guid Id,
    string Name,
    NotificationChannel Channel,
    int RecipientCount,
    DateTimeOffset CreatedAt,
    CampaignStatus Status);

// Dữ liệu client được phép gửi lên: không có Id, thời gian, trạng thái (server tự sinh).
// Người nhận chọn từ danh bạ (ContactIds), nhập trực tiếp (Recipients) hoặc cả hai.
public record RecipientInput(string? Name, string? Address);

public record CreateCampaignRequest(
    string? Name,
    NotificationChannel Channel,
    string? Subject,
    string? Content,
    List<int>? ContactIds,
    List<RecipientInput>? Recipients);

// Cấu trúc lỗi thống nhất cho mọi phản hồi lỗi. Errors liệt kê lỗi theo từng trường khi dữ liệu sai.
public record ApiError(string Code, string Message, Dictionary<string, string[]>? Errors = null);
