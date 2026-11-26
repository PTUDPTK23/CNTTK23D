# Đề tài 20 – Hệ thống thư viện và đặt mượn sách phân tán

- **Sinh viên:** Vũ Duy Linh – dtc245200402
- **Lớp:** Phát triển ứng dụng phân tán – CNTT K23D

## 1. Bài toán

Xây dựng hệ thống thư viện cho phép người dùng tra cứu sách, gửi yêu cầu đặt mượn và theo dõi trạng thái xử lý. Yêu cầu đặt mượn được xử lý bất đồng bộ qua RabbitMQ để kiểm tra số bản còn khả dụng và xác nhận việc giữ sách.

```text
Client -> Library API -> RabbitMQ -> Reservation Worker -> Database
              |                                               |
              +------------------> Database <-----------------+
```

Chức năng chính:

1. Tra cứu danh sách và chi tiết sách.
2. Tra cứu số bản đang khả dụng.
3. Gửi yêu cầu đặt mượn sách.
4. Đưa yêu cầu vào RabbitMQ để xử lý.
5. Worker kiểm tra số bản khả dụng và xác nhận hoặc từ chối yêu cầu.
6. Cập nhật trạng thái `Pending`, `Reserved`, `Rejected`, `Borrowed`, `Returned`.
7. Tra cứu trạng thái yêu cầu đặt mượn.
8. Tra cứu danh sách sách đang mượn hoặc đã đặt của một người dùng.

## 2. Mẫu trao đổi thông điệp (MEP)

Theo bản đồ 5 MEP của học phần, đề tài dùng **Qua hàng đợi (Queue-based)** làm MEP chính: *giao việc, xử lý sau*.

| MEP | Vai trò trong đề tài |
|---|---|
| Yêu cầu–phản hồi | Library Client ↔ Library API (REST over HTTP) cho tra cứu sách, tạo yêu cầu đặt mượn, tra cứu trạng thái, mượn và trả sách |
| Thông điệp một chiều | Nằm trong Queue-based: API đẩy `ReservationMessage` vào queue rồi trả `201 Created` ngay mà không chờ worker xử lý xong |
| **Qua hàng đợi** | **Chính:** `LibraryApi` → RabbitMQ (`reservation_requests`) → `ReservationWorker` nhận và xử lý yêu cầu đặt mượn, mỗi yêu cầu chỉ một worker xử lý |
| Phát hành–đăng ký | Không dùng cho đặt mượn (tránh nhiều worker cùng xử lý một yêu cầu gây trừ trùng `AvailableCopies`); có thể mở rộng cho sự kiện thông báo / thống kê |
| Truyền luồng / đẩy | Tuỳ chọn mở rộng: đẩy cập nhật trạng thái từ `Pending` sang `Reserved`/`Rejected` lên giao diện client |

**Lý do chọn Queue-based:**

- API phản hồi ngay cho người dùng (`201 Created` với trạng thái `Pending`), không phải chờ bước kiểm tra tồn kho và giữ sách.
- Hàng đợi `reservation_requests` đóng vai trò bộ đệm khi có nhiều lượt đặt mượn đồng thời.
- Có thể tăng số lượng `ReservationWorker` để tăng thông lượng; mỗi thông điệp chỉ giao cho một worker (`prefetchCount: 1`, ACK thủ công).
- Queue `durable: true` và thông điệp `Persistent = true` kết hợp ACK thủ công giúp tránh mất yêu cầu khi worker gặp sự cố.

**Vì sao không phải Publish–subscribe:** Mỗi lượt đặt mượn là một yêu cầu giao dịch cần được xử lý đúng một lần (kiểm tra và trừ `AvailableCopies` cho một người dùng cụ thể), nên phải phân phối kiểu điểm-điểm qua hàng đợi (*competing consumers*) thay vì phát tán bản sao tới tất cả subscriber.

## 3. Kiến trúc hệ thống

| Thành phần | Công nghệ | Vai trò |
|---|---|---|
| Library API | ASP.NET Core Minimal API (.NET 10), `RabbitMQ.Client` | Cung cấp REST API tra cứu sách, tạo yêu cầu đặt mượn (`Pending`), gửi thông điệp vào RabbitMQ, cập nhật mượn/trả sách; tích hợp Swagger |
| Reservation Worker | .NET Worker Service (.NET 10), `RabbitMQ.Client` | Lắng nghe queue `reservation_requests`, kiểm tra số bản khả dụng trong transaction, cập nhật `Reserved` hoặc `Rejected` |
| Library Client | .NET Console App (.NET 10), `HttpClient` | Ứng dụng dòng lệnh gọi tuần tự các luồng API (`GET`, `POST`) và kiểm thử các tình huống `200`/`201`/`400`/`404` qua HTTP |
| Shared | .NET Class Library (.NET 10), EF Core | Thư viện dùng chung: Entity models, DTOs, `ReservationMessage`, `LibraryDbContext` và dữ liệu seed |
| Message broker | RabbitMQ (`rabbitmq:4-management-alpine`) | Hàng đợi `reservation_requests` trung gian giữa `LibraryApi` và `ReservationWorker` |
| Database | PostgreSQL (`postgres:17-alpine`) + EF Core (`Npgsql`) | Lưu trữ sách, người dùng và yêu cầu đặt mượn |
| Triển khai | Docker, Docker Compose | Mỗi service có `Dockerfile` riêng; tách cấu hình kết nối ra biến môi trường trong `docker-compose.yml` |

## 4. Dữ liệu chính

| Bảng (`DbSet`) | Nội dung |
|---|---|
| `Books` | Thông tin sách: `Id`, `Title`, `Author`, `ISBN` (unique), `Description`, `TotalCopies`, `AvailableCopies`, `CreatedAt` |
| `Users` | Thông tin người dùng: `Id`, `Name`, `Email`, `CreatedAt` |
| `Reservations` | Yêu cầu đặt mượn: `Id`, `UserId`, `BookId`, `Status` (lưu chuỗi tối đa 20 ký tự), `CreatedAt`, `UpdatedAt` |

Vòng đời trạng thái yêu cầu đặt mượn (`ReservationStatus`):

```text
Pending ──► Reserved ──► Borrowed ──► Returned
   │
   └──────► Rejected
```

- `Pending`: Vừa tạo qua `POST /reservations`, đang chờ Worker xử lý.
- `Reserved`: Worker xác nhận còn bản khả dụng (`AvailableCopies > 0`), đã trừ `AvailableCopies` và giữ sách.
- `Rejected`: Worker từ chối vì sách không tồn tại hoặc hết bản khả dụng (`AvailableCopies <= 0`).
- `Borrowed`: Người dùng đã nhận sách (`PUT /reservations/{id}/borrow`).
- `Returned`: Người dùng đã trả sách (`PUT /reservations/{id}/return`), đồng thời tăng lại `AvailableCopies`.

### Dữ liệu mẫu được seed sẵn (`LibraryDbContext`)

- **Người dùng (`Users`):**
  1. Nguyễn Văn An (`nva@email.com`)
  2. Trần Thị Bình (`ttb@email.com`)
  3. Lê Văn Cường (`lvc@email.com`)
- **Sách (`Books`):**
  1. `Clean Code` – Robert C. Martin (`ISBN: 978-0132350884`, `TotalCopies: 3`)
  2. `Design Patterns` – Gang of Four (`ISBN: 978-0201633610`, `TotalCopies: 2`)
  3. `The Pragmatic Programmer` – David Thomas, Andrew Hunt (`ISBN: 978-0135957059`, `TotalCopies: 4`)
  4. `Introduction to Algorithms` – Thomas H. Cormen (`ISBN: 978-0262033848`, `TotalCopies: 2`)
  5. `Distributed Systems` – Andrew S. Tanenbaum (`ISBN: 978-1543057386`, `TotalCopies: 3`)

## 5. API tối thiểu

Các endpoint tối thiểu của đề tài và các endpoint mở rộng cho quy trình mượn/trả:

| Method | Path | Mô tả |
|---|---|---|
| `GET` | `/books` | Lấy danh sách tất cả sách và số bản khả dụng |
| `GET` | `/books/{id}` | Lấy chi tiết một đầu sách theo `id` |
| `POST` | `/reservations` | Tạo yêu cầu đặt mượn (`Pending`) và đẩy vào RabbitMQ |
| `GET` | `/reservations/{id}` | Tra cứu trạng thái xử lý của một yêu cầu đặt mượn |
| `GET` | `/users/{userId}/reservations` | Lấy danh sách yêu cầu đặt mượn/đang mượn của một người dùng |
| `PUT` | `/reservations/{id}/borrow` | Mở rộng: chuyển trạng thái từ `Reserved` sang `Borrowed` |
| `PUT` | `/reservations/{id}/return` | Mở rộng: chuyển trạng thái từ `Borrowed` sang `Returned` và cộng lại `AvailableCopies` |

## 6. Các vấn đề phân tán cần xử lý

- **Tránh giữ sách vượt quá số bản khả dụng (Race Condition):** Khi nhiều yêu cầu đến gần nhau, `ReservationProcessorService` xử lý từng message (`BasicQosAsync` với `prefetchCount: 1`) trong database transaction kết hợp khóa dòng `SELECT * FROM "Books" WHERE "Id" = {0} FOR UPDATE`.
- **Không xử lý trùng khi RabbitMQ giao lại message (*at-least-once*):** Trước khi trừ `AvailableCopies`, Worker kiểm tra `reservation.Status == ReservationStatus.Pending`; nếu đã ở trạng thái khác thì bỏ qua và ACK.
- **Không mất thông điệp khi Worker lỗi hoặc restart:** Queue `reservation_requests` đặt `durable: true`, thông điệp đặt `Persistent = true`; Worker bật `autoAck: false`, chỉ gọi `BasicAckAsync` sau khi commit DB và gọi `BasicNackAsync(..., requeue: true)` khi có ngoại lệ.
- **Chịu lỗi khi Database / RabbitMQ khởi động chậm hoặc gián đoạn:**
  - `LibraryApi` và `ReservationWorker` có vòng lặp retry kết nối PostgreSQL (15 lần, mỗi lần 3 giây) và RabbitMQ (`LibraryApi`: 10 lần x 3 giây; `ReservationWorker`: 20 lần x 5 giây).
  - Trong `POST /reservations`, bản ghi `Pending` được lưu vào PostgreSQL trước khi publish sang RabbitMQ; nếu bước publish lỗi, API ghi log lỗi để phục vụ đối soát/xử lý bù.
  - `docker-compose.yml` cấu hình `healthcheck` cho `postgres` và `rabbitmq`, `depends_on` theo `service_healthy`, và `restart: on-failure`.
- **Hướng mở rộng tiếp theo:** Bổ sung Outbox Worker quét định kỳ các `Reservation` còn ở `Pending` nhưng chưa publish được khi RabbitMQ dừng, và cấu hình Retry Queue / Dead-Letter Queue (DLQ) thay cho `requeue: true` vô hạn khi thông điệp bị lỗi định dạng.

## 7. Kết quả tuần 1

Giai đoạn hiện tại: **Bài 1–2** (Minimal API + RESTful API lưu dữ liệu trong bộ nhớ + client console). Các thành phần `Shared` (PostgreSQL/EF Core), `ReservationWorker` (RabbitMQ) và `docker-compose.yml` được chuẩn bị sẵn để tích hợp hoàn chỉnh ở các tuần sau.

### Thành phần

- `src/LibraryApi`: Web API (ASP.NET Core Minimal API, .NET 10), lưu dữ liệu trong bộ nhớ thông qua `LibraryStore`.
- `src/LibraryClient`: ứng dụng console gọi tuần tự các API của `LibraryApi` qua HTTP và kiểm thử xử lý lỗi (`200`, `201`, `400`, `404`, lỗi kết nối).
- `src/Shared` & `src/ReservationWorker`: khung dữ liệu EF Core và Worker Service chuẩn bị cho giai đoạn tích hợp PostgreSQL + RabbitMQ.

### Cách chạy

```bash
dotnet build Duan.slnx
```

Terminal 1:

```bash
dotnet run --project src/LibraryApi --urls http://localhost:5000
```

Terminal 2:

```bash
dotnet run --project src/LibraryClient -- http://localhost:5000
```

### Endpoint và mã trạng thái đã cài đặt

| Method | URI | Kết quả trả về trong `LibraryApi` |
| --- | --- | --- |
| `GET` | `/` | `200`, `"LibraryApi đang chạy"` |
| `GET` | `/users` | `200`, danh sách người dùng mẫu (`UserDto`) |
| `GET` | `/books` | `200`, danh sách sách (`BookDto`) |
| `GET` | `/books?availableOnly=...` | `200`, hoặc `400` `INVALID_FILTER` |
| `GET` | `/books/{id:int}` | `200` (`BookDto`), hoặc `404` `BOOK_NOT_FOUND` |
| `POST` | `/reservations` | `201` + header `Location: /reservations/{id}` (`Status: "Pending"`), hoặc `400` `INVALID_RESERVATION` (gom lỗi từng trường `userId`, `bookId`) |
| `GET` | `/reservations/{id:int}` | `200` (`ReservationDto`), hoặc `404` `RESERVATION_NOT_FOUND` |
| `GET` | `/reservations?status=...` | `200`, hoặc `400` `INVALID_STATUS` |
| `GET` | `/users/{userId:int}/reservations` | `200` (`ReservationDto[]`), hoặc `404` `USER_NOT_FOUND` |
| `PUT` | `/reservations/{id:int}/borrow` | `200`, `400` `INVALID_STATUS_TRANSITION`, hoặc `404` `RESERVATION_NOT_FOUND` |
| `PUT` | `/reservations/{id:int}/return` | `200`, `400` `INVALID_STATUS_TRANSITION`, hoặc `404` `RESERVATION_NOT_FOUND` |

### Kết quả chạy máy khách

```text
=== 1. Xem danh sách sách ===
  [1] Clean Code - Robert C. Martin (Khả dụng: 3/3)
  [2] Design Patterns - Gang of Four (Khả dụng: 2/2)
  [3] The Pragmatic Programmer - David Thomas, Andrew Hunt (Khả dụng: 4/4)
  [4] Introduction to Algorithms - Thomas H. Cormen (Khả dụng: 2/2)
  [5] Distributed Systems - Andrew S. Tanenbaum (Khả dụng: 3/3)
  Chi tiết sách [1]: Clean Code | ISBN: 978-0132350884 | A Handbook of Agile Software Craftsmanship

=== 2. Tạo yêu cầu đặt mượn ===
  Status: 201 Created
  Location: /reservations/1
  Yêu cầu #1 | Sách: Clean Code | Người dùng: Nguyễn Văn An | Trạng thái: Pending | Lúc: 16:29:27

=== 5. Gửi yêu cầu đặt mượn sai (mong đợi 400) ===
  Status: 400 BadRequest
  Lỗi 400 - INVALID_RESERVATION: Dữ liệu đặt mượn không hợp lệ.
    userId: UserId phải lớn hơn 0.
    bookId: Không có sách 99 trong thư viện.

=== 6. Tra yêu cầu không tồn tại (mong đợi 404) ===
  GET /reservations/999999 → 404 NotFound
  Lỗi 404 - RESERVATION_NOT_FOUND: Không tìm thấy yêu cầu đặt mượn 999999.

=== 7. Gọi sai route (mong đợi 404 không có ApiError) ===
  GET /reservations/abc → 404 NotFound
  Lỗi 404 (không có ApiError trong phản hồi)
```

### Kịch bản kiểm thử trong [`requests.http`](requests.http)

- `GET /`, `GET /users`, `GET /books`, `GET /books?availableOnly=true` (`200 OK`) và `GET /books?availableOnly=abc` (`400 INVALID_FILTER`).
- `GET /books/1` (`200 OK`) và `GET /books/999` (`404 BOOK_NOT_FOUND`).
- `POST /reservations` hợp lệ (`201 Created` + `Location`) và dữ liệu sai `{ "userId": 0, "bookId": 99 }` (`400 INVALID_RESERVATION`).
- `GET /reservations/1` (`200 OK`), `GET /reservations/999999` (`404 RESERVATION_NOT_FOUND`), `GET /reservations/abc` (`404` rỗng không body).
- `GET /users/1/reservations` (`200 OK`) và `GET /users/999/reservations` (`404 USER_NOT_FOUND`).
