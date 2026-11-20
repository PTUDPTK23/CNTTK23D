# Đề tài 11 – Hệ thống gửi thông báo hàng loạt · Tuần 1

- **Sinh viên:** Nguyễn Văn Phương – dtc245200722
- **Lớp:** Phát triển ứng dụng phân tán – CNTT K23D
- **Mã nguồn dự án:** https://github.com/VPhuong06/nguyenvanphuong-he_thong_thong_bao

## 1. Bài toán

Xây dựng hệ thống cho phép tạo chiến dịch thông báo đến nhiều người nhận. Campaign API chỉ tiếp nhận và tạo chiến dịch; các thông báo cụ thể được chia thành job và xử lý bất đồng bộ bởi nhiều Notification Worker.

```
Admin Client -> Campaign API -> RabbitMQ -> Notification Workers
                     |                         |
                     +------> Database <-------+
```

Chức năng chính:

1. Tạo chiến dịch thông báo.
2. Chọn hoặc nhập danh sách người nhận.
3. Sinh các job gửi thông báo và đưa vào RabbitMQ.
4. Nhiều worker nhận và xử lý job độc lập.
5. Ghi nhận kết quả thành công hoặc thất bại của từng thông báo.
6. Theo dõi số lượng Pending, Sent, Failed của chiến dịch.
7. Tra cứu chi tiết kết quả gửi.
8. Chạy lại các job thất bại theo cơ chế phù hợp.

## 2. Mẫu trao đổi thông điệp (MEP)

Theo bản đồ 5 MEP của học phần, đề tài dùng **Qua hàng đợi (Queue-based)** làm MEP chính: *giao việc, xử lý sau*.

| MEP | Vai trò trong đề tài |
|---|---|
| Yêu cầu–phản hồi | Admin Client ↔ Campaign API (REST) |
| Thông điệp một chiều | Nằm trong Queue-based: API đẩy job rồi không chờ |
| **Qua hàng đợi** | **Chính:** Campaign API → RabbitMQ (`notify.jobs`) → nhiều worker cạnh tranh nhận job, mỗi job chỉ một worker xử lý |
| Phát hành–đăng ký | Không dùng cho job (sẽ gửi trùng N lần); dùng cho event kết quả gửi phục vụ thống kê |
| Truyền luồng / đẩy | Tuỳ chọn: cập nhật tiến độ lên giao diện |

Lý do chọn Queue-based: API trả về ngay (không chờ gửi xong); hàng đợi làm bộ đệm khi chiến dịch lớn; tăng số worker để tăng tốc; queue durable và ack thủ công đảm bảo không mất job; retry queue và dead-letter queue phục vụ chạy lại job lỗi.

**Vì sao không phải Publish–subscribe:** gửi cho nhiều người nhưng mỗi người nhận là một job riêng (nội dung cá nhân hoá, trạng thái và retry riêng), người nhận do admin chọn chứ không tự đăng ký kênh.

## 3. Kiến trúc dự kiến

| Thành phần | Công nghệ | Vai trò |
|---|---|---|
| Campaign API | ASP.NET Core Minimal API (.NET 10) | Tạo, xem, start chiến dịch; tra cứu kết quả; chạy lại job lỗi |
| Notification Worker | .NET Worker Service, RabbitMQ.Client | Nhận job, gửi thông báo, ghi kết quả; chạy nhiều instance |
| Admin Web | Blazor Web App | Giao diện quản trị |
| Message broker | RabbitMQ | Hàng đợi job, retry, dead-letter |
| Database | PostgreSQL + EF Core | Chiến dịch, người nhận, job, lịch sử gửi |
| Triển khai | Docker, Docker Compose | Mỗi service một Dockerfile; cấu hình qua biến môi trường |

## 4. Dữ liệu chính

| Bảng | Nội dung |
|---|---|
| `campaigns` | Chiến dịch: tên, kênh, tiêu đề, nội dung mẫu (`{{name}}`), trạng thái |
| `contacts` | Danh bạ để chọn người nhận |
| `campaign_recipients` | Danh sách người nhận của chiến dịch |
| `deliveries` | Job gửi: người nhận, trạng thái Pending/Processing/Sent/Failed, số lần thử, lỗi gần nhất, worker xử lý |
| `delivery_attempts` | Lịch sử từng lần gửi |

Trạng thái chiến dịch: `Draft → Running → Completed / CompletedWithFailures`.

## 5. API tối thiểu

| Method | Path | Mô tả |
|---|---|---|
| POST | `/campaigns` | Tạo chiến dịch |
| GET | `/campaigns` | Danh sách chiến dịch |
| GET | `/campaigns/{id}` | Chi tiết và số lượng Pending/Sent/Failed |
| POST | `/campaigns/{id}/start` | Sinh job và đưa vào RabbitMQ |
| GET | `/campaigns/{id}/deliveries` | Kết quả gửi chi tiết |

## 6. Các vấn đề phân tán cần xử lý

- **Không mất job khi RabbitMQ dừng:** ghi job vào DB trước (outbox), publish sau và publish bù khi broker chạy lại.
- **Không gửi trùng:** RabbitMQ đảm bảo at-least-once, nên worker claim job bằng câu UPDATE có điều kiện trước khi gửi.
- **Worker chết giữa chừng:** ack thủ công sau khi ghi kết quả; job đang xử lý dở được nhận lại sau khi hết lease.
- **Đếm đúng khi nhiều worker chạy song song:** đếm trực tiếp từ bảng `deliveries`.
- **Chạy lại job lỗi:** phân loại lỗi tạm thời / vĩnh viễn; tự thử lại có backoff, hết lượt thì vào dead-letter queue; admin chạy lại thủ công.

## 7. Kết quả Tuần 1

Giai đoạn hiện tại: **Bài 1–2** (Minimal API + RESTful API + client console). Hàng đợi RabbitMQ, Notification Worker và cơ sở dữ liệu được bổ sung ở các tuần sau.

- Phân tích yêu cầu đề tài, chọn MEP Queue-based; thiết kế kiến trúc, dữ liệu và API như trên.
- Hiện thực lát cắt đầu tiên: API quản lý chiến dịch và máy khách console gọi API qua HTTP.

### Thành phần

- `src/Campaign.Api`: Web API (ASP.NET Core Minimal API, .NET 10), lưu dữ liệu trong bộ nhớ.
- `src/Campaign.Client`: ứng dụng console gọi API qua HTTP.

### Cách chạy

```
dotnet build NotificationSystem.slnx
```

Terminal 1:

```
dotnet run --project src/Campaign.Api --urls http://localhost:5000
```

Terminal 2:

```
dotnet run --project src/Campaign.Client -- http://localhost:5000
```

### Endpoint

| Method | URI | Kết quả |
| --- | --- | --- |
| GET | `/` | 200, API đang chạy |
| GET | `/contacts` | 200, danh bạ người nhận |
| POST | `/campaigns` | 201 + Location, hoặc 400 `INVALID_CAMPAIGN` (kèm lỗi từng trường) |
| GET | `/campaigns/{id}` | 200, hoặc 404 `CAMPAIGN_NOT_FOUND` |
| GET | `/campaigns?channel=...` | 200, hoặc 400 `INVALID_CHANNEL` |

### Kết quả chạy máy khách

```
=== 2. Tạo chiến dịch ===
  Status: 201 Created
  Location: /campaigns/dca4d7c0-e674-4080-8519-399bde1dbb2e
  Chiến dịch dca4d7c0-… | Thông báo lịch thi | Kênh: Email | Trạng thái: Draft
    - Người nhận 01 <nguoinhan01@example.com>
    - Người nhận 02 <nguoinhan02@example.com>
    - Nguyễn Văn An <an@example.com>

=== 5. Gửi chiến dịch sai (mong đợi 400) ===
  Status: 400 BadRequest
  Lỗi 400 - INVALID_CAMPAIGN: Dữ liệu chiến dịch không hợp lệ.
    name: Thiếu tên chiến dịch.
    subject: Thiếu tiêu đề.
    content: Thiếu nội dung thông báo.
    contactIds: Không có liên hệ 99 trong danh bạ.
    recipients: Địa chỉ không hợp lệ với kênh Email: khong-phai-email

=== 6. Tra chiến dịch không tồn tại (mong đợi 404) ===
  Lỗi 404 - CAMPAIGN_NOT_FOUND: Không tìm thấy chiến dịch 34fd292f-….

=== 7. Gọi sai route (mong đợi 404 không có ApiError) ===
  GET /campaigns/abc → 404 NotFound
  Lỗi 404 (không có ApiError trong phản hồi)
```

### Bảng phá thử

| # | Thao tác | Mong đợi | Thực tế | Giải thích |
| --- | --- | --- | --- | --- |
| 1 | `POST /campaigns` hợp lệ | 201 + Location | 201, `Location: /campaigns/{id}` | Tạo tài nguyên mới, Location chỉ URI đọc lại |
| 2 | POST thiếu tên, tiêu đề, nội dung, liên hệ không có, địa chỉ sai | 400 | 400 `INVALID_CAMPAIGN`, đủ 5 lỗi theo trường | Kiểm tra tại biên, gom mọi lỗi để client sửa một lần |
| 3 | `GET /campaigns/{guid không tồn tại}` | 404 + ApiError | 404 `CAMPAIGN_NOT_FOUND` | Handler chạy, không tìm thấy tài nguyên |
| 4 | `GET /campaigns/abc` | 404 rỗng | 404, không có body | Không khớp ràng buộc `{id:guid}`, framework trả 404 trước handler |
| 5 | `GET /campaigns?channel=Fax` | 400 | 400 `INVALID_CHANNEL` | Điều kiện lọc trong query string cũng phải kiểm tra |
| 6 | `PUT /campaigns/{id}` | 405 | 405, `Allow: GET` | URI tồn tại, chỉ phương thức không được hỗ trợ |
| 7 | Bỏ header `Content-Type` | 415 | 415 | Framework không biết body là JSON |
| 8 | Body JSON hỏng | 400 | 400, không có body | Framework không đọc được body |
| 9 | Tắt API rồi chạy client | Lỗi kết nối | `No connection could be made… (localhost:5000)` | Lỗi mạng: không có phản hồi HTTP, khác lỗi 4xx |
| 10 | Dừng rồi chạy lại API, đọc chiến dịch cũ | 404 | 404 `CAMPAIGN_NOT_FOUND` | Dữ liệu nằm trong `List<T>` của tiến trình, dừng là mất (thêm cơ sở dữ liệu ở tuần sau) |
