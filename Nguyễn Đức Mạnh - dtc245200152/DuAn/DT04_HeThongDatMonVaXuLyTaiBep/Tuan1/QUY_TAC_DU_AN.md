# Quy tắc dự án – Đề tài 04: Hệ thống đặt món và xử lý tại bếp

> Bản đề xuất, chờ duyệt. Áp dụng cho mọi lần viết hoặc sửa code trong dự án.
> Giai đoạn hiện tại: **Bài 1–2** (Minimal API + RESTful API + client console).

## 0. Quy tắc bắt buộc của chủ dự án

1. Trước khi ra quyết định hay sửa, xoá, thay đổi bất cứ gì trong dự án, phải có quyết định của chủ dự án.
2. Không sửa những phần không được yêu cầu, và **CHỈ** sửa đúng phần được yêu cầu.
3. Code không lan man, đúng trọng tâm, bám đúng yêu cầu dự án.

## 2. Cấu trúc thư mục

```
OrderSystem/
├── OrderSystem.slnx
├── README.md
└── src/
    ├── Order.Api/        (dotnet new web)
    │   ├── Program.cs
    │   ├── Dtos.cs
    │   ├── Models.cs
    │   └── OrderStore.cs
    └── Order.Client/     (dotnet new console)
        ├── Program.cs
        └── Dtos.cs
```

- Solution dùng định dạng `.slnx`, project đặt trong `src/`.
- API và Client là **hai tiến trình độc lập**, chỉ nói chuyện qua HTTP.
- Client **không tham chiếu** project API. Client tự khai báo record DTO có cùng hình dạng JSON.

## 3. Quy ước đặt tên C#

| Loại | Quy ước | Ví dụ |
| --- | --- | --- |
| Class, record, enum, method, property | PascalCase | `OrderStore`, `CreateOrder()`, `CustomerName` |
| Biến cục bộ, tham số | camelCase | `orderId`, `request` |
| Trường private | `_camelCase` | `_orders` |
| DTO trả về | hậu tố `Dto` | `OrderDto`, `MenuItemDto` |
| DTO gửi lên | hậu tố `Request` | `CreateOrderRequest` |
| Mã lỗi trong ApiError | CHỮ_HOA_GẠCH_DƯỚI | `ORDER_NOT_FOUND`, `INVALID_ORDER` |

- Tên biến, hàm, class viết **tiếng Anh**. Chú thích (comment) và chuỗi in ra màn hình viết **tiếng Việt**.
- Bật `Nullable` và `ImplicitUsings` (mặc định của template). Không để cảnh báo nullable.
- Dùng `record` cho DTO. Dùng `async`/`await` cho mọi lời gọi mạng.
- Chú thích ngắn ở chỗ có ý nghĩa với bài học (ví dụ vì sao trả 201, vì sao kiểm tra status code). Không chú thích những dòng hiển nhiên.

## 4. Quy tắc phía API (Order.Api)

### 4.1. Program.cs theo 2 pha

```
CreateBuilder → đăng ký service (builder.Services) → Build
→ map endpoint (MapGet/MapPost) → Run
```

### 4.2. Endpoint của giai đoạn này

| Method | URI | Thành công | Lỗi |
| --- | --- | --- | --- |
| GET | `/` | 200, thông báo API đang chạy | |
| GET | `/menu` | 200, danh sách `MenuItemDto` | |
| POST | `/orders` | 201 + `Location: /orders/{id}` + `OrderDto` | 400 `ApiError` |
| GET | `/orders/{id:guid}` | 200, `OrderDto` | 404 `ORDER_NOT_FOUND` |
| GET | `/orders?customer=...&status=...` | 200, danh sách `OrderDto` | 400 nếu `status` không hợp lệ |

### 4.3. REST

- URI là **danh từ** tài nguyên (`/orders`, `/menu`); method HTTP diễn đạt hành động.
- **Định danh** đặt trong path (`/orders/{id:guid}`); **lọc, tìm kiếm** đặt trong query string.
- Body chỉ dùng cho POST/PUT/PATCH.
- Status code đúng ngữ nghĩa: 200, 201, 400, 404. Dùng `Results.Ok`, `Results.Created(uri, dto)`, `Results.BadRequest`, `Results.NotFound`.

### 4.4. DTO và dữ liệu

- Ở biên API chỉ nhận và trả **DTO**, không trả trực tiếp model nội bộ.
- `CreateOrderRequest` **không chứa Id, thời gian, trạng thái**: server tự sinh.
- Id dùng `Guid.NewGuid()`, thời gian dùng `DateTimeOffset.UtcNow`.
- Trạng thái đơn là enum: `Pending`, `Preparing`, `Ready`, `Cancelled`. Đơn mới luôn là `Pending`. Enum trả về dạng chuỗi trong JSON.
- Dữ liệu lưu bằng `List<T>` trong một service đăng ký **Singleton** (`OrderStore`). Dùng `lock` khi thêm hoặc đọc danh sách đơn.
- Menu là dữ liệu mẫu cố định trong code.

### 4.5. Kiểm tra dữ liệu và lỗi

- Kiểm tra **tại biên**, trước khi vào logic nghiệp vụ.
- Mọi lỗi trả cùng một dạng: `ApiError(string Code, string Message)`.
- Các lỗi tối thiểu:

| Trường hợp | Status | Code |
| --- | --- | --- |
| Thiếu tên khách | 400 | `INVALID_ORDER` |
| Không có món nào | 400 | `INVALID_ORDER` |
| Số lượng ≤ 0 | 400 | `INVALID_ORDER` |
| Món không có trong menu | 400 | `MENU_ITEM_NOT_FOUND` |
| Không tìm thấy đơn | 404 | `ORDER_NOT_FOUND` |

- `Message` viết tiếng Việt, nói rõ sai ở đâu.

## 5. Quy tắc phía Client (Order.Client)

- Địa chỉ API lấy từ **tham số dòng lệnh** (`args[0]`), mặc định `http://localhost:5000`. Gán vào `HttpClient.BaseAddress`.
- Với lời gọi có thể lỗi: giữ `HttpResponseMessage`, **kiểm tra `StatusCode` trước** rồi mới đọc body. Nhánh thành công đọc DTO, nhánh lỗi đọc `ApiError`.
- Phân biệt **lỗi kết nối** (`HttpRequestException`: API chưa chạy, sai địa chỉ) với **lỗi HTTP** (400, 404).
- Kịch bản chạy theo thứ tự, mỗi bước in tiêu đề rõ ràng:
    1. Xem menu.
    2. Đặt một đơn hợp lệ, in status code và `Location`.
    3. Đọc lại đơn vừa tạo bằng `GET /orders/{id}`.
    4. Lọc đơn theo tên khách.
    5. Cố ý gửi đơn sai, in `ApiError` (400).
    6. Tra một id không tồn tại, in `ApiError` (404).

## 6. Cách chạy

```
dotnet build OrderSystem.slnx
dotnet run --project src/Order.Api --urls http://localhost:5000
dotnet run --project src/Order.Client -- http://localhost:5000
```

API và Client chạy ở **hai terminal riêng**.

## 7. Quy trình làm việc

1. Trước khi code: nói rõ sẽ làm gì, thuộc bài nào, và chờ duyệt nếu là phần mới.
2. Làm từng phần nhỏ, mỗi phần build và chạy được.
3. Khi sang bài mới: cập nhật mục "Giai đoạn hiện tại" trong file này, giữ code cũ chạy được, **không làm vỡ hợp đồng API đã có**.
4. Nếu dùng Git: mỗi commit một thay đổi có ý nghĩa, message tiếng Việt ngắn gọn (ví dụ `Thêm endpoint GET /orders/{id}`).
5. README ghi cách chạy và danh sách endpoint, cập nhật khi thêm phần mới.

## 8. Việc chưa làm ở giai đoạn này

Database, SignalR, GraphQL, gRPC, RabbitMQ, Kitchen Worker, Notification Service, Docker, xác thực, giao diện web. Sẽ thêm khi tới bài tương ứng.
