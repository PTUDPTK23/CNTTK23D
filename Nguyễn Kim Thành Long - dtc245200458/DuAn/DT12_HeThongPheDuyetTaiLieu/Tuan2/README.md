# Đề tài 12 – Hệ thống phê duyệt tài liệu phân tán (Tuần 2 – 3 – 4)

## 1. Tổng quan kiến trúc hệ thống

Giải pháp `DocumentApproval.slnx` được xây dựng trên nền tảng **.NET 10** gồm 2 thành phần:

- **`Document.Api`** (ASP.NET Core Minimal API + Static Web Dashboard):
  - **EF Core + SQLite (`document_approval.db`)**: Tự động khởi tạo bảng bằng `EnsureCreated()` khi ứng dụng chạy, bật chế độ `PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;` để xử lý ghi đồng thời an toàn và lưu trữ bền vững sau khi tắt/mở lại server.
  - **Optimistic Concurrency Control**: Thực thể `DocumentEntity` sử dụng thuộc tính `Version` gắn `[ConcurrencyCheck]`. Mọi thao tác chuyển trạng thái đều tăng `Version` và kiểm tra xung đột (`ExpectedVersion` + `DbUpdateConcurrencyException`), đảm bảo khi nhiều người duyệt cùng tác động vào một tài liệu tại một thời điểm thì chỉ **1 yêu cầu đầu tiên thành công (HTTP 200)**, các yêu cầu đến sau nhận **HTTP 409 Conflict**.
  - **Idempotency & Audit Trail**: Hỗ trợ `Idempotency-Key` (qua HTTP Header hoặc payload) và lưu đầy đủ lịch sử chuyển trạng thái (`HistoryId`, `Action`, `FromStatus`, `ToStatus`, `PerformedBy`, `Note`, `Timestamp`).
  - **Web Dashboard (`wwwroot/index.html`)**: Giao diện trực quan dùng HTML5 + TailwindCSS + Vanilla JS cho phép thao tác toàn bộ vòng đời tài liệu, xem Timeline lịch sử và bấm nút **Test Xung Đột Đồng Thời** ngay trên trình duyệt.

- **`Document.Client`** (Console App dùng `HttpClient`):
  - Chạy kịch bản kiểm thử tự động toàn diện: Tạo 2 tài liệu → Gửi duyệt → Duyệt tài liệu 1 → Từ chối tài liệu 2 → Giả lập 2 luồng đồng thời (`Task.WhenAll`) cùng xử lý 1 tài liệu → In toàn bộ Audit Trail ra màn hình Console.

## 2. Sơ đồ State Machine

```text
  [POST /documents]
         │
         ▼
      (Draft) ──[POST /documents/{id}/submit]──► (Submitted)
                                                     │
                        ┌────────────────────────────┴────────────────────────────┐
                        │                                                         │
        [POST /documents/{id}/approve]                            [POST /documents/{id}/reject]
                        │                                           (Bắt buộc có Note lý do)
                        ▼                                                         ▼
                   (Approved)                                                (Rejected)
```

Mọi chuyển trạng thái sai quy tắc (ví dụ: `Approve` khi đang là `Draft`, hoặc `Submit` khi đã `Approved`) đều bị từ chối với **HTTP 409 Conflict**.

## 3. Danh sách 6 API RESTful

| Phương thức | Endpoint | Mô tả | Mã phản hồi chính |
| :--- | :--- | :--- | :--- |
| `POST` | `/documents` | Tạo tài liệu mới ở trạng thái `Draft` (kèm bản ghi `History`: `Created`) | `201 Created`, `400 Bad Request` |
| `GET` | `/documents` | Lấy danh sách tài liệu (lọc tùy chọn `?status=...&approver=...`) | `200 OK`, `400 Bad Request` |
| `GET` | `/documents/{id}` | Lấy chi tiết tài liệu kèm toàn bộ `History` theo trình tự thời gian | `200 OK`, `404 Not Found` |
| `POST` | `/documents/{id}/submit` | Chuyển `Draft` → `Submitted` (ghi `History`: `Submitted`) | `200 OK`, `404`, `409 Conflict` |
| `POST` | `/documents/{id}/approve` | Chuyển `Submitted` → `Approved` (ghi `History`: `Approved`) | `200 OK`, `404`, `409 Conflict` |
| `POST` | `/documents/{id}/reject` | Chuyển `Submitted` → `Rejected` (bắt buộc có `Note` lý do từ chối) | `200 OK`, `400`, `404`, `409 Conflict` |

## 4. Hướng dẫn build và chạy demo

Mở terminal tại thư mục `Tuan2`:

### Bước 1: Build solution

```powershell
dotnet build DocumentApproval.slnx
```

### Bước 2: Chạy `Document.Api` (Terminal 1)

```powershell
dotnet run --project Document.Api
```

- API mặc định lắng nghe tại: `http://localhost:5001`
- Mở trình duyệt truy cập **`http://localhost:5001`** để sử dụng **Web Dashboard Demo Trực Quan** (tạo tài liệu, gửi duyệt, phê duyệt/từ chối, xem Timeline History và bấm nút **⚡ Test Xung Đột Đồng Thời**).

### Bước 3: Chạy `Document.Client` (Terminal 2)

```powershell
dotnet run --project Document.Client
```

(Có thể đặt biến môi trường `$env:DOCUMENT_API_URL = "http://localhost:5001"` nếu API chạy ở cổng khác.)
