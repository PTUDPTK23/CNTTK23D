// Client không tham chiếu code của API.
// Các record dưới đây chỉ cần cùng hình dạng JSON với DTO phía API.

public record ContactDto(int Id, string Name, string Email, string Phone);

public record RecipientDto(string Name, string Address);

// Kênh và trạng thái nhận về dạng chuỗi, ví dụ "Email", "Draft"
public record CampaignDto(
    Guid Id,
    string Name,
    string Channel,
    string Subject,
    string Content,
    List<RecipientDto> Recipients,
    DateTimeOffset CreatedAt,
    string Status);

public record CampaignSummaryDto(
    Guid Id,
    string Name,
    string Channel,
    int RecipientCount,
    DateTimeOffset CreatedAt,
    string Status);

public record RecipientInput(string? Name, string? Address);

public record CreateCampaignRequest(
    string? Name,
    string Channel,
    string? Subject,
    string? Content,
    List<int>? ContactIds,
    List<RecipientInput>? Recipients);

public record ApiError(string Code, string Message, Dictionary<string, string[]>? Errors);
