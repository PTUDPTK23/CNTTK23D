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

- Phân tích yêu cầu đề tài, chọn MEP Queue-based.
- Thiết kế kiến trúc, dữ liệu và API như trên.
- Dựng khung solution .NET 10 (Campaign.Api, Notification.Worker, Admin.Web, Notification.Shared), Dockerfile cho từng service và `docker-compose.yml`.
