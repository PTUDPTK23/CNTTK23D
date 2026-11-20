using System.Net.Mail;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

// ===== Pha 1: chuẩn bị =====
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CampaignStore>();

// Trả enum dạng chuỗi ("Email", "Draft") thay vì số
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// ===== Pha 2: map endpoint và chạy =====

// Kiểm tra API đang chạy
app.MapGet("/", () => Results.Ok("Campaign.Api đang chạy"));

// Danh bạ để chọn người nhận
app.MapGet("/contacts", (CampaignStore store) =>
    Results.Ok(store.GetContacts().Select(ToContactDto)));

// Tạo chiến dịch: dữ liệu lấy từ body
app.MapPost("/campaigns", (CreateCampaignRequest request, CampaignStore store) =>
{
    // Kiểm tra dữ liệu tại biên, trước khi vào nghiệp vụ. Gom đủ lỗi của mọi trường để client sửa một lần.
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Name))
        errors["name"] = ["Thiếu tên chiến dịch."];
    if (string.IsNullOrWhiteSpace(request.Subject))
        errors["subject"] = ["Thiếu tiêu đề."];
    if (string.IsNullOrWhiteSpace(request.Content))
        errors["content"] = ["Thiếu nội dung thông báo."];

    var recipients = new List<Recipient>();
    var seen = new HashSet<string>();
    var invalid = new List<string>();

    void Add(int? contactId, string? name, string? address)
    {
        var normalized = NormalizeAddress(request.Channel, address);
        if (normalized is null)
        {
            invalid.Add(string.IsNullOrWhiteSpace(address) ? $"{name} (thiếu địa chỉ)" : address);
            return;
        }
        // Bỏ người nhận trùng thay vì báo lỗi
        if (!seen.Add(normalized))
            return;

        recipients.Add(new Recipient
        {
            ContactId = contactId,
            Name = string.IsNullOrWhiteSpace(name) ? normalized : name.Trim(),
            Address = normalized
        });
    }

    foreach (var contactId in request.ContactIds ?? [])
    {
        var contact = store.FindContact(contactId);
        if (contact is null)
        {
            errors["contactIds"] = [$"Không có liên hệ {contactId} trong danh bạ."];
            continue;
        }
        Add(contact.Id, contact.Name, request.Channel == NotificationChannel.Email ? contact.Email : contact.Phone);
    }

    foreach (var input in request.Recipients ?? [])
        Add(null, input.Name, input.Address);

    if (invalid.Count > 0)
        errors["recipients"] = [$"Địa chỉ không hợp lệ với kênh {request.Channel}: {string.Join(", ", invalid)}"];
    else if (recipients.Count == 0 && !errors.ContainsKey("contactIds"))
        errors["recipients"] = ["Chiến dịch cần ít nhất một người nhận."];

    if (errors.Count > 0)
        return Results.BadRequest(new ApiError("INVALID_CAMPAIGN", "Dữ liệu chiến dịch không hợp lệ.", errors));

    // Server tự sinh Id, thời gian và trạng thái ban đầu
    var campaign = new Campaign
    {
        Id = Guid.NewGuid(),
        Name = request.Name!.Trim(),
        Channel = request.Channel,
        Subject = request.Subject!.Trim(),
        Content = request.Content!,
        Recipients = recipients,
        CreatedAt = DateTimeOffset.UtcNow,
        Status = CampaignStatus.Draft
    };
    store.AddCampaign(campaign);

    // 201 Created + header Location + DTO trong body
    return Results.Created($"/campaigns/{campaign.Id}", ToCampaignDto(campaign));
});

// Chi tiết chiến dịch: định danh nằm trong path
app.MapGet("/campaigns/{id:guid}", (Guid id, CampaignStore store) =>
{
    var campaign = store.FindCampaign(id);
    return campaign is null
        ? Results.NotFound(new ApiError("CAMPAIGN_NOT_FOUND", $"Không tìm thấy chiến dịch {id}."))
        : Results.Ok(ToCampaignDto(campaign));
});

// Danh sách chiến dịch: điều kiện lọc nằm trong query string
app.MapGet("/campaigns", (string? channel, CampaignStore store) =>
{
    NotificationChannel? channelFilter = null;
    if (channel is not null)
    {
        if (!Enum.TryParse<NotificationChannel>(channel, true, out var parsed) || !Enum.IsDefined(parsed))
            return Results.BadRequest(new ApiError("INVALID_CHANNEL",
                $"Kênh '{channel}' không hợp lệ. Dùng: Email, Sms."));
        channelFilter = parsed;
    }

    return Results.Ok(store.GetCampaigns(channelFilter).Select(ToSummaryDto));
});

app.Run();

// ===== Kiểm tra địa chỉ theo kênh gửi =====
// Trả địa chỉ đã chuẩn hoá (để loại trùng không phụ thuộc chữ hoa/thường), hoặc null nếu không hợp lệ.
static string? NormalizeAddress(NotificationChannel channel, string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        return null;

    var value = raw.Trim();
    if (channel == NotificationChannel.Email)
        return MailAddress.TryCreate(value, out var mail) && mail.Address == value ? value.ToLowerInvariant() : null;

    var phone = Regex.Replace(value, @"[\s.\-()]", "");
    return Regex.IsMatch(phone, @"^\+?[0-9]{8,15}$") ? phone : null;
}

// ===== Chuyển model nội bộ sang DTO =====
static ContactDto ToContactDto(Contact c) => new(c.Id, c.Name, c.Email, c.Phone);

static CampaignDto ToCampaignDto(Campaign c) => new(
    c.Id,
    c.Name,
    c.Channel,
    c.Subject,
    c.Content,
    c.Recipients.Select(r => new RecipientDto(r.Name, r.Address)).ToList(),
    c.CreatedAt,
    c.Status);

static CampaignSummaryDto ToSummaryDto(Campaign c) =>
    new(c.Id, c.Name, c.Channel, c.Recipients.Count, c.CreatedAt, c.Status);
