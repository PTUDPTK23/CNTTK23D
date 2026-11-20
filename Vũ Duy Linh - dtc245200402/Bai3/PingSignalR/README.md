# Bài 3 – Giao tiếp thời gian thực với SignalR

**Họ và tên:** Vũ Duy Linh  
**MSSV:** DTC245200402  
**Lớp:** CNTT K23D  
**Học phần:** Phát triển ứng dụng phân tán  

---

## Cấu trúc thư mục và Giải pháp

Thư mục bài làm bao gồm hai solution độc lập:

| Solution | Project | Cổng mặc định | Mục đích |
|---|---|:---:|---|
| `PingSignalR` (Phần 2) | `Ping.Api`, `Ping.Client` | `5200` | Nắm vững cơ chế kết nối, negotiate, scope gửi tin (Caller, All, Group), vòng đời kết nối và xử lý lỗi SignalR |
| `RealtimeSubmissionSystem` (Phần 3) | `Submission.RealtimeApi`, `Submission.RealtimeClient` | `5000` | Tích hợp SignalR vào hệ thống nộp bài EduSubmit: REST tạo & đọc dữ liệu, SignalR đẩy trạng thái nền qua `IHubContext` |

Toàn bộ ảnh chụp kết quả kiểm thử được lưu trữ tại `docs/img/` (chụp trực tiếp từ terminal khi chạy thực tế).

---

## Phần 2 – PingSignalR

### 1. Hướng dẫn chạy

Khởi động lần lượt qua các terminal:

```bash
# Terminal 1: Chạy máy chủ Ping.Api
cd PingSignalR
dotnet run --project src/Ping.Api --urls http://localhost:5200

# Terminal 2: Chạy máy khách An
dotnet run --project src/Ping.Client -- http://localhost:5200 An

# Terminal 3: Chạy máy khách Binh
dotnet run --project src/Ping.Client -- http://localhost:5200 Binh
```

---

### 2. Các bước triển khai & Kết quả thực nghiệm

#### Bước 1 – Tạo Solution và thiết lập Project
- Tạo solution `PingSignalR.sln`, khởi tạo project máy chủ Web API `Ping.Api` và project máy khách Console `Ping.Client`.
- Cài đặt gói thư viện máy khách `Microsoft.AspNetCore.SignalR.Client` cho `Ping.Client`.
- Lệnh `dotnet build` hoàn thành thành công 0 lỗi.

![Bước 1 – Build thành công](docs/img/ping-b1-build.png)

#### Bước 2 – Cấu hình Hub SignalR và kiểm tra Negotiate
- Cấu hình SignalR trên máy chủ: `builder.Services.AddSignalR()` và ánh xạ endpoint `app.MapHub<PingHub>("/hubs/ping")`.
- Kiểm thử các endpoint bằng `curl`:
  - `GET /` trả về `{"service":"PingApi","status":"running"}` xác nhận API hoạt động bình thường.
  - `POST /hubs/ping/negotiate?negotiateVersion=1` trả về `connectionId`, `connectionToken` và 3 phương thức truyền tải: `WebSockets`, `ServerSentEvents`, `LongPolling`.
  - `GET /hubs/ping` trả về mã lỗi `400 Bad Request – Connection ID required`: Xác minh Hub SignalR không phục vụ như một endpoint HTTP REST thông thường mà bắt buộc phải qua bước bắt tay (handshake/negotiate) trước.

![Bước 2 – Kiểm tra Negotiate](docs/img/ping-b2-negotiate.png)

#### Bước 3 & Bước 4 – Xây dựng phương thức Echo và gọi từ máy khách
- Trong `PingHub`, triển khai phương thức `Echo(string message)` gửi ngược lại qua `Clients.Caller.SendAsync("EchoResponse", ...)`.
- Phía máy khách `Ping.Client`, kết nối qua `HubConnectionBuilder`, đăng ký lắng nghe qua `connection.On<string>("EchoResponse", ...)` và gọi `connection.InvokeAsync("Echo", ...)`.
- **Kết quả:** Máy khách An in ra console:
  ```text
  [Echo] Echo: Xin chao SignalR tu An
  ```
  Thông điệp phản hồi chỉ trả về cho chính người gọi (`Clients.Caller`), các máy khách khác không nhận được.

![Bước 4 – Kiểm tra Echo](docs/img/ping-b4-echo.png)

#### Bước 5 – Phát rộng (Broadcast) và Nhóm (Group)
- Mở rộng Hub với các phương thức:
  - `Broadcast(string message)`: Gửi đến toàn bộ kết nối thông qua `Clients.All`.
  - `JoinGroup(string groupName)`: Thêm kết nối vào nhóm qua `Groups.AddToGroupAsync`.
  - `SendToGroup(string groupName, string message)`: Gửi thông điệp đến các kết nối trong nhóm qua `Clients.Group(groupName)`.
- Chạy máy chủ, mở kết nối An, sau đó mở kết nối Bình:
  - Cửa sổ của An nhận thông báo Bình vào nhóm: `[Group] ... da tham gia nhom lop-hoc-01`.
  - An nhận được thông điệp nhóm và broadcast từ Bình.
  - An **không** thấy tin nhắn Echo của Bình.
  - Bình không nhìn thấy tin nhắn do An gửi trước thời điểm Bình kết nối.

![Bước 5 – Kiểm tra Broadcast và Nhóm](docs/img/ping-b5-an-binh.png)

#### Bước 6 – Vòng đời kết nối và tự động kết nối lại
- Ghi đè `OnConnectedAsync` và `OnDisconnectedAsync` trên `PingHub` để máy chủ theo dõi và thông báo khi máy khách kết nối/ngắt kết nối.
- Ở máy khách, sử dụng `.WithAutomaticReconnect()` và lắng nghe các sự kiện `Reconnecting`, `Reconnected`, `Closed`.
- **Thử nghiệm:** Tắt máy chủ đột ngột khi An đang chạy, sau đó bật lại máy chủ.
- **Hiện tượng:** Máy khách in `[Lifecycle] Dang ket noi lai...` rồi `[Lifecycle] Da ket noi lai: <ConnectionId_Moi>`.
- **Nhận xét:** ConnectionId sau khi kết nối lại bị thay đổi. Máy chủ xem đây là một kết nối mới, do đó các nhóm mà máy khách đã tham gia trước đó bị mất và máy khách cần chủ động tham gia lại (re-join group).

![Bước 6 – Tự động kết nối lại](docs/img/ping-b6-ket-noi-lai.png)

#### Bước 7 – Bảng phá thử nghiệm 3 tình huống lỗi

| # | Thao tác cố tình gây lỗi | Hiện tượng thực tế | Nguyên nhân & Bài học rút ra |
|---|---|---|---|
| **TN1** | Chạy máy khách `Ping.Client` khi máy chủ `Ping.Api` chưa chạy | Ném ra `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5200)` | Phương thức `StartAsync()` ném lỗi ngay lập tức vì không thể mở kết nối lần đầu. `WithAutomaticReconnect()` chỉ có tác dụng khôi phục các kết nối **đã được thiết lập thành công** trước đó. |
| **TN2** | Sửa URL kết nối máy khách thành `/hub/ping` (thiếu ký tự `s`) | Ném ra `HttpRequestException: Response status code does not indicate success: 404 (Not Found)` tại bước `NegotiateAsync` | Lỗi xảy ra ngay ở bước negotiate. Đường dẫn client gọi phải khớp chính xác tuyệt đối với cấu hình `app.MapHub(...)` trên server. |
| **TN3** | Sửa tên lắng nghe máy khách từ `EchoResponse` thành `EchoRespone` | Máy khách chạy bình thường, kết nối thành công nhưng hoàn toàn không thấy dòng in `[Echo]` | Sai tên sự kiện chiều từ máy chủ đẩy xuống (Push event name). SignalR không ném ngoại lệ khi tên không khớp, khiến thông điệp bị bỏ qua trong im lặng. Cần chuẩn hóa tên event bằng hằng số chung. |

![TN1 – Máy chủ tắt](docs/img/ping-b7-tn1-may-chu-tat.png)
![TN2 – Sai đường dẫn Hub](docs/img/ping-b7-tn2-sai-duong-dan.png)
![TN3 – Sai tên thông điệp](docs/img/ping-b7-tn3-sai-ten.png)

---

### 3. Phân biệt 4 phạm vi gửi trong SignalR

| Phạm vi | Đối tượng nhận | Trường hợp sử dụng điển hình trong EduSubmit |
|---|---|---|
| `Clients.All` | Tất cả mọi máy khách đang kết nối tới Hub | Thông báo toàn hệ thống bảo trì; thông báo có bài tập mới được đăng tải |
| `Clients.Caller` | Duy nhất máy khách vừa thực hiện lời gọi Hub | Phản hồi xác nhận: "Bạn đã đăng ký theo dõi lượt nộp thành công" |
| `Clients.Group(name)` | Các máy khách đã tham gia vào nhóm `name` | Đẩy trạng thái bài nộp theo mã lượt nộp: nhóm `submission:{id}` |
| `Clients.Client(connId)` | Một kết nối cụ thể qua định danh `connectionId` | Gửi riêng thông điệp bảo mật hoặc cảnh báo đến phiên làm việc cá nhân |

---

## Phần 3 – RealtimeSubmissionSystem (EduSubmit thời gian thực)

### 1. Bốn câu hỏi thiết kế hệ thống

| # | Câu hỏi thiết kế | Lựa chọn kiến trúc & Giải pháp triển khai |
|---|---|---|
| **①** | **Sự kiện nào kích hoạt gửi thông điệp?** | Mỗi khi trạng thái lượt nộp thay đổi (`received` → `processing` → `graded`) bên trong hàm nền `SimulateGradingAsync` thông qua lời gọi `UpdateStatusAsync`. |
| **②** | **Gửi cho ai?** | `Clients.Group("submission:{id}")`: Chỉ các máy khách đăng ký theo dõi đúng lượt nộp bài đó mới nhận được thông báo, tránh phát tán thừa thông tin sang sinh viên khác. |
| **③** | **Gửi kèm dữ liệu gì?** | DTO `SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt)`: Gửi đúng thông tin cần thiết về mã bài nộp, trạng thái mới và dấu thời gian cập nhật. |
| **④** | **Mất kết nối thì lấy lại trạng thái thế nào?** | Máy khách gọi lại API REST: `GET /api/submissions/{id}`. Cơ sở dữ liệu / REST API luôn là **nguồn sự thật duy nhất (Single Source of Truth)**. SignalR đóng vai trò kênh thông báo tức thời. |

> **Điểm mấu chốt:** Tác vụ chấm bài nền chạy ngoài vòng đời của một Hub Request, do đó máy chủ sử dụng **`IHubContext<SubmissionHub>`** được inject từ Dependency Injection container để đẩy thông điệp từ bên ngoài Hub.

---

### 2. Hướng dẫn chạy

```bash
# Terminal 1: Chạy máy chủ RealtimeApi
cd RealtimeSubmissionSystem
dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000

# Terminal 2: Chạy máy khách SV001
dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV001
```

---

### 3. Các bước triển khai & Kết quả thực nghiệm

#### Bước 1 – Tạo Solution
Khởi tạo solution `RealtimeSubmissionSystem.sln` với `Submission.RealtimeApi` (ASP.NET Core Web API) và `Submission.RealtimeClient` (Console Client). Build thành công.

![Bước 1 – Build thành công](docs/img/rt-b1-build.png)

#### Bước 2 – REST API kết hợp Thread-safe (lock) và UpdatedAt
- Đăng ký endpoint `POST /api/assignments/{id}/submissions` trả về mã `201 Created` kèm header `Location`.
- Body trả về gồm trạng thái ban đầu `"received"` và thời gian `"updatedAt"`.
- Dùng `lock (gate)` bảo vệ danh sách `submissions` trong bộ nhớ tránh xung đột đa luồng.

![Bước 2 – POST 201 Created](docs/img/rt-b2-post-201.png)

#### Bước 3 – SubmissionHub và nhóm theo dõi `submission:{id}`
- Khai báo `SubmissionHub` với phương thức `WatchSubmission(Guid submissionId)` đưa máy khách vào nhóm `SubmissionGroups.ForSubmission(submissionId)`.
- Ánh xạ Hub tại endpoint `/hubs/submissions`.
- Kiểm tra negotiate tại `/hubs/submissions/negotiate` thành công.

![Bước 3 – Negotiate SubmissionHub](docs/img/rt-b3-negotiate.png)

#### Bước 4 – Chấm bài nền với IHubContext
- Sau khi nhận bài nộp, kích hoạt tác vụ nền `_ = SimulateGradingAsync(sub.Id, submissions, gate, hubContext)`.
- Trạng thái tự động cập nhật: `received` → sau 1 giây thành `processing` → sau 2 giây tiếp theo thành `graded`.
- Kiểm tra 3 lần `GET /api/submissions/{id}` cho thấy dữ liệu cập nhật chính xác và máy chủ ghi nhật ký `[Push]` ra màn hình.

![Bước 4 – Kiểm tra đổi trạng thái](docs/img/rt-b4-get-doi-trang-thai.png)

#### Bước 5 & Bước 6 – Luồng phối hợp hoàn chỉnh trên máy khách
Máy khách Console phối hợp chặt chẽ giữa SignalR và REST API:
1. Kết nối SignalR (`StartAsync`).
2. Đăng ký lắng nghe sự kiện `"SubmissionStatusChanged"`.
3. Gọi REST `POST /api/assignments/1/submissions` để tạo lượt nộp.
4. Gọi Hub `WatchSubmission(submissionId)` để vào nhóm theo dõi.
5. Chờ thông báo SignalR và khi nhận được `graded`, gọi lại REST `GET /api/submissions/{id}` để đối soát dữ liệu.

### Bước 7. Bảng phá thử nghiệm 5 tình huống phân tán

| # | Thí nghiệm phân tán | Kết quả thực tế quan sát được | Bản chất kỹ thuật & Giải thích |
|---|---|---|---|
| **TN1** | Chạy 2 máy khách SV001 và SV002 cùng lúc | Máy khách SV001 chỉ nhận thông báo bài nộp của SV001; SV002 chỉ nhận thông báo của SV002 | Nhóm `submission:{id}` phân tách các kênh thông báo độc lập. Máy chủ chỉ định tuyến thông điệp tới thành viên trong nhóm, bảo mật thông tin và tiết kiệm băng thông. |
| **TN2** | Chạy máy khách khi máy chủ chưa bật | `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5000)` | Kết nối khởi đầu không thể thiết lập. Client phải đón lỗi này để nhắc người dùng hoặc thử lại có chu kỳ. |
| **TN3** | Máy khách gọi sai đường dẫn Hub (`/hub/...`) | Lỗi `404 Not Found` ngay tại pha negotiate | URL giữa máy khách và cấu hình `MapHub` trên máy chủ không đồng nhất. |
| **TN4** | Đổi tên sự kiện máy khách lắng nghe (khác `"SubmissionStatusChanged"`) | Client dừng mãi ở dòng *"Dang cho thong bao..."* cho đến khi hết thời gian chờ | Tên sự kiện bị lệch làm SignalR bỏ qua thông điệp mà không báo lỗi. |
| **TN5** | Đóng máy chủ khi bài đang ở trạng thái `processing` | Client báo `Reconnecting`, sau khi bật lại server thì nhận được trạng thái cuối qua bước đọc lại REST `GET` | Sau khi reconnect, client đã mất nhóm cũ trong SignalR. Tuy nhiên, kiến trúc REST làm nguồn sự thật giúp client đọc lại đúng trạng thái `graded` mà không bị treo. |

![TN1 – Hai máy khách độc lập](docs/img/rt-b7-tn1-hai-may-khach.png)  
![TN2 – Máy chủ chưa chạy](docs/img/rt-b7-tn2-may-chu-chua-chay.png)  
![TN3 – Sai đường dẫn Hub](docs/img/rt-b7-tn3-sai-duong-dan.png)  
![TN4 – Sai tên sự kiện](docs/img/rt-b7-tn4-sai-ten-su-kien.png)  
![TN5 – Tự phục hồi trạng thái](docs/img/rt-b7-tn5-mat-mang.png)  

**Kết quả màn hình máy khách (8 dòng hiển thị chuẩn):**
```text
Da ket noi SignalR.
Chon bai tap: 1 - Bai tap SignalR
Da tao luot nop: 8ec5e6b8-6c99-435f-88d3-b96c2a7455ef
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: 8ec5e6b8-6c99-435f-88d3-b96c2a7455ef -> processing luc 08:51:35
Thong bao SignalR: 8ec5e6b8-6c99-435f-88d3-b96c2a7455ef -> graded luc 08:51:37
Trang thai doc lai bang REST: graded
```

## Phần 4 – Trả lời câu hỏi ôn tập & thảo luận

## Trả lời 3 câu hỏi đầu giờ

1. **Máy chủ có thể chủ động báo không?** Có, nếu máy khách mở sẵn một kết nối lâu dài (WebSocket qua SignalR). Máy chủ gửi bằng `Clients.Group(...).SendAsync`; khi ở ngoài hub (tác vụ chấm nền) thì dùng `IHubContext<SubmissionHub>`.
2. **REST và SignalR chia việc thế nào?** REST tạo và đọc tài nguyên với mã trạng thái rõ ràng (201, 400, 404). SignalR chỉ đẩy các thay đổi phát sinh sau yêu cầu ban đầu (processing, graded). SignalR không thay thế REST.
3. **Đứt mạng đúng lúc báo graded thì sao?** Tin đó mất, SignalR không lưu lại. `WithAutomaticReconnect` nối lại với ConnectionId mới (đã thấy ở Bước 6 Phần 2), nhóm cũ mất; máy khách phải `WatchSubmission` lại và gọi `GET /api/submissions/{id}` để lấy trạng thái cuối. Thí nghiệm 4 và 5 cho thấy REST vẫn đọc đúng `graded` dù không nhận được tin đẩy.
