# Đề tài 12 – Hệ thống phê duyệt tài liệu phân tán (Tuần 1)

## 1. Kiến trúc Tuần 1

Trong phạm vi Tuần 1, giải pháp `DocumentApproval.slnx` gồm 2 project trên nền tảng **.NET 10**:

- **`Document.Api`** (ASP.NET Core Minimal API):
  - Cung cấp các REST API quản lý tài liệu và lịch sử xử lý.
  - Mô hình dữ liệu (`Document.Api/Models/DocumentModels.cs`):
    - `DocumentItem`: `Id`, `Title`, `Content`, `Creator`, `Approver`, `Status`, `CreatedAt`, `UpdatedAt`.
    - `DocumentHistory`: `Id`, `DocumentId`, `Action`, `PerformedBy`, `Note`, `Timestamp`.
    - `DocumentStatus`: hỗ trợ tối thiểu 4 trạng thái `Draft`, `Submitted`, `Approved`, `Rejected` (Tuần 1 khởi tạo mặc định là `Draft`).
  - Lưu trữ In-Memory Thread-Safe (`Document.Api/Services/InMemoryDocumentStore.cs`):
    - Đăng ký dạng Singleton (`IDocumentStore` -> `InMemoryDocumentStore`) sử dụng `ConcurrentDictionary` kết hợp khóa đồng bộ cho danh sách lịch sử từng tài liệu.
    - Khởi tạo sẵn 2 tài liệu mẫu ở trạng thái `Draft` kèm lịch sử `Created` để có thể chạy thử ngay.

- **`Document.Client`** (Console App dùng `HttpClient`):
  - Tự động đọc URL của API từ biến môi trường `DOCUMENT_API_URL` (mặc định: `http://localhost:5001`).
  - Thực hiện kịch bản demo tuần tự:
    1. Tạo 2 tài liệu mới (`POST /documents`).
    2. Lấy danh sách toàn bộ tài liệu (`GET /documents`).
    3. Xem chi tiết 1 tài liệu kèm toàn bộ lịch sử xử lý (`GET /documents/{id}`).

## 2. Danh sách API đã hoàn thành

| Phương thức | Endpoint | Mô tả |
| :--- | :--- | :--- |
| `POST` | `/documents` | Tạo tài liệu mới ở trạng thái `Draft` (nhận `Title`, `Content`, `Creator`, `Approver`), tự động ghi 1 bản ghi lịch sử `Created`. Trả về `201 Created`. |
| `GET` | `/documents` | Lấy danh sách tất cả tài liệu. Hỗ trợ query tùy chọn `?status=...&approver=...` để lọc theo trạng thái (`Draft`, `Submitted`, `Approved`, `Rejected`) và người phê duyệt. |
| `GET` | `/documents/{id}` | Lấy chi tiết 1 tài liệu theo `Guid` kèm toàn bộ danh sách lịch sử xử lý (`History`). Trả về `404 Not Found` nếu không tồn tại. |

## 3. Hướng dẫn build và chạy

Mở terminal tại thư mục `Tuan1`:

### Bước 1: Build toàn bộ solution

```powershell
dotnet build DocumentApproval.slnx
```

### Bước 2: Chạy `Document.Api` (Terminal 1)

```powershell
dotnet run --project Document.Api
```

Mặc định API lắng nghe tại `http://localhost:5001` (theo `Document.Api/Properties/launchSettings.json`).

### Bước 3: Chạy `Document.Client` (Terminal 2)

```powershell
dotnet run --project Document.Client
```

Nếu muốn trỏ tới URL/cổng khác, thiết lập biến môi trường `DOCUMENT_API_URL` trước khi chạy:

```powershell
$env:DOCUMENT_API_URL = "http://localhost:5001"
dotnet run --project Document.Client
```
