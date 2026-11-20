# Nhật ký thực hành Bài 3: Giao tiếp thời gian thực với SignalR
git add "Lưu Văn Thành - dtc245200351/Bai3/README.md"
git commit -m "Bai3: cap nhat README.md va link anh"
git push origin main
## Bước 1: Tạo solution và 2 project PingSignalR
![buoc1-build](anh/buoc1-build.png)
- **Thấy:** `dotnet build` báo `Build succeeded` cho cả `Ping.Api` và `Ping.Client`.
- **Giải thích:** Đã tạo solution chứa 2 project và thêm gói `Microsoft.AspNetCore.SignalR.Client` phiên bản 10.0.15 cho phía máy khách.
cd D:\Code\Bai3
git add .
git commit -m "Bai3 B2: cau hinh PingHub va kiem tra negotiate"
## Bước 2: Cấu hình Hub phía máy chủ và kiểm tra negotiate
![buoc2-negotiate](anh/buoc2-negotiate.png)
- **Thấy:** `POST /hubs/ping/negotiate` trả về JSON chứa `connectionId` và danh sách `availableTransports` (`WebSockets`, `ServerSentEvents`, `LongPolling`). `GET /hubs/ping` trả về mã lỗi `400 Connection ID required`.
- **Giải thích:** Hub SignalR đã được ánh xạ thành công tại `/hubs/ping` và yêu cầu bắt tay (negotiate) lấy `connectionId` trước khi thiết lập kết nối. 
## Bước 3: Phương thức Echo, máy chủ trả lời riêng người vừa gọi
![buoc3-echo](anh/buoc3-echo.png)
- **Thấy:** Đã thêm phương thức `Echo(string message)` vào `PingHub` sử dụng `Clients.Caller.SendAsync("EchoResponse", ...)`. Lệnh `dotnet build` báo `Build succeeded` (`0 Error(s)`) và máy chủ chạy ổn định tại cổng `5200`.
- **Giải thích:** `Clients.Caller` chỉ gửi thông điệp phản hồi `"EchoResponse"` về duy nhất kết nối vừa gọi `Echo`, các client khác không nhận được.
## Bước 4: Máy khách kết nối Hub và gọi phương thức Echo
![buoc4-client-echo](anh/buoc4-client-echo.png)
- **Thấy:** Client `Thanh` kết nối thành công tới `PingApi`, gọi `InvokeAsync("Echo", ...)` và nhận ngay phản hồi `[Echo] Echo: Xin chao SignalR tu Thanh`. Khi thử sửa sai tên ở chiều lên (`"Ech0"`), chương trình báo lỗi `HubException: Method does not exist` và dừng; khi sửa sai ở chiều xuống (`"EchoRespone"`), chương trình không báo lỗi nhưng không nhận được dữ liệu.
- **Giải thích:** Tên phương thức khi gọi (`InvokeAsync`) và tên sự kiện lắng nghe (`On`) phải khớp chính xác từng ký tự giữa Client và Hub.
## Bước 5: Thêm phát rộng và nhóm, ba phạm vi gửi trong cùng một Hub
![buoc5-scopes](anh/buoc5-scopes.png)
- **Thấy:** Khi chạy `An` trước và `Bình` sau, cửa sổ `An` nhận được thông báo nhóm (`[Group]`) và phát rộng (`[Broadcast]`) của `Bình`, nhưng không nhận được phản hồi `[Echo]` của `Bình`.
- **Giải thích:** `Clients.Caller` chỉ gửi riêng cho người gọi; `Clients.Group` gửi cho các thành viên đang ở trong nhóm (kể cả khi người gửi không thuộc nhóm đó); `Clients.All` gửi tới toàn bộ kết nối đang mở.

### Đáp án thử thách 3 máy khách (An, Bình thuộc `lop-hoc-01`; Chi không thuộc nhóm nào):
| Lời gọi | An | Bình | Chi | Phạm vi |
|---|---|---|---|---|
| 1. An gọi `Echo("hi")` | Nhận | Không | Không | `Clients.Caller` |
| 2. Chi gọi `SendToGroup("lop-hoc-01", "x")` | Nhận | Nhận | Không (vì Chi không ở trong nhóm) | `Clients.Group` |
| 3. Bình gọi `Broadcast("y")` | Nhận | Nhận | Nhận | `Clients.All` |
## Bước 6: Vòng đời kết nối và tự động kết nối lại
![buoc6-reconnect](anh/buoc6-reconnect.png)
- **Thấy:** Máy chủ in `[Hub] + ... ket noi` khi client mở và `[Hub] - ... ngat` khi đóng. Khi tắt máy chủ rồi bật lại, client `An` tự động kích hoạt sự kiện `Reconnecting` (`Dang ket noi lai...`) và `Reconnected` (`Da ket noi lai: ...`) với một `ConnectionId` mới.
- **Giải thích:** `WithAutomaticReconnect()` mặc định thử kết nối lại tại các mốc `0s, 2s, 10s, 30s` (hết 4 lần sẽ chuyển sang `Closed`). Sau khi nối lại thành công, server cấp `ConnectionId` mới nên client cần tự đăng ký lại vào các nhóm cũ nếu cần.

## Bước 7: Ba thí nghiệm lỗi kết nối phân tán
![buoc7-loi1](anh/buoc7-loi1.png)
1. **Máy chủ chưa chạy:** Chạy client khi server đã tắt $\rightarrow$ Gặp lỗi `HttpRequestException: Connection refused (localhost:5200)` ngay tại `StartAsync()` vì cơ chế tự nối lại chỉ áp dụng cho kết nối đã thiết lập thành công trước đó.
2. **Sai đường dẫn Hub (`/hub/ping`):** Gặp lỗi `404 (Not Found)` trong pha `negotiate` do URL không khớp tuyến đã khai báo ở `MapHub<PingHub>("/hubs/ping")`.
3. **Sai tên thông điệp (`On("EchoRespone")`):** Không báo lỗi biên dịch hay runtime nhưng không nhận được phản hồi `[Echo]` do tên sự kiện lắng nghe không khớp với tên gửi từ Hub.

## Tổng hợp 4 phạm vi gửi thông điệp trong SignalR
| Phạm vi | Người nhận | Ví dụ ứng dụng (EduSubmit) |
|---|---|---|
| `Clients.All` | Mọi kết nối đang mở (bao gồm cả người gửi) | Thông báo máy chủ bảo trì; bài tập mới được đăng |
| `Clients.Caller` | Chỉ kết nối vừa gọi phương thức trên Hub | Xác nhận đã đăng ký theo dõi lượt nộp |
| `Clients.Group(ten)` | Toàn bộ thành viên đang ở trong nhóm | Cập nhật trạng thái chấm bài của nhóm `submission:{id}` |
| `Clients.Client(id)` | Duy nhất một `ConnectionId` cụ thể | Gửi thông báo riêng cho một phiên làm việc |
## Phần 3: Hệ thống nộp bài thời gian thực (EduSubmit Realtime)

### 1. Kết quả chạy thực nghiệm & Kiểm thử
* **Kiểm tra Build hệ thống (Bước 1):**
  ![Build Solution](anh/edusubmit-b1-build.png)

* **Kiểm tra POST tạo lượt nộp REST API (Bước 2):**
  ![POST Submission](anh/edusubmit-b2-post.png)

* **Kiểm tra SignalR Negotiate đúng & sai đường dẫn (Bước 3):**
  ![SignalR Negotiate](anh/edusubmit-b3.png)

* **Kiểm tra tác vụ nền đổi trạng thái & luồng máy khách thời gian thực (Bước 4 - 6):**
  ![Client Realtime](anh/edusubmit-client.png)

* **Kiểm thử tự động với `requests.http` (Bước 8):**
  ![Requests HTTP](anh/edusubmit-requests.png)

---

### 2. Trả lời 3 câu hỏi đầu giờ
1. **Tại sao không dùng mỗi REST mà cần thêm SignalR?**
   * REST hoạt động theo cơ chế Request–Response (máy khách phải chủ động hỏi liên tục/polling để biết bài đã chấm xong chưa, gây tốn băng thông và có độ trễ). SignalR mở kênh kết nối hai chiều (WebSockets) giúp máy chủ chủ động đẩy (push) ngay lập tức sự kiện đổi trạng thái (`received` $\rightarrow$ `processing` $\rightarrow$ `graded`) tới đúng máy khách đang chờ.
2. **Tại sao không bỏ REST để dùng mỗi SignalR?**
   * REST chuẩn hóa tốt các thao tác CRUD tài nguyên qua HTTP (tạo lượt nộp `POST` trả về `201 Created` kèm `Location` và mã `id` định danh, truy vấn trạng thái ban đầu `GET`, xử lý lỗi chuẩn `400`/`404` và dễ kiểm thử/cache). SignalR chỉ giữ vai trò truyền thông báo thời gian thực mỏng nhẹ.
3. **Nếu máy khách lỡ mất thông báo SignalR (do rớt mạng tạm thời hoặc kết nối vào nhóm trễ) thì làm sao biết trạng thái mới nhất?**
   * Máy khách luôn giữ kênh REST (`GET /api/submissions/{id}`) làm nguồn dữ liệu gốc (source of truth). Ngay sau khi đăng ký nhóm SignalR (`WatchSubmission`) và khi kết nối lại (`Reconnected`), máy khách gọi lại API REST để đọc trạng thái mới nhất trên máy chủ và dùng trường `UpdatedAt` để bỏ qua các thông báo cũ hơn.

---

### 3. Bảng kết quả 5 thí nghiệm phân tán (Bước 7)

| STT | Thí nghiệm | Dự đoán | Kết quả thực tế | Giải thích bản chất phân tán |
| :--- | :--- | :--- | :--- | :--- |
| **1** | **Hai máy khách nộp 2 bài khác nhau cùng lúc** | Mỗi máy khách chỉ nhận thông báo của riêng bài mình nộp. | Đúng như dự đoán (`edusubmit-b7-exp.png`): mỗi client nhận riêng luồng `processing` $\rightarrow$ `graded` của đúng GUID mình tạo. | Hub đưa từng kết nối vào nhóm riêng `submission:{id}`, giúp cô lập phạm vi phát tin (multicast theo nhóm). |
| **2** | **Tắt máy chủ giữa chừng khi máy khách đang chờ** | Máy khách báo mất kết nối hoặc kích hoạt tự động kết nối lại. | Máy khách phát hiện đứt kết nối ngay khi tiến trình Server dừng; dữ liệu trong bộ nhớ (`List`) của Server bị mất do chưa lưu DB. | Giao tiếp mạng không tin cậy và lưu trữ In-Memory không bền vững qua các lần khởi động lại tiến trình. |
| **3** | **Gọi `WatchSubmission` với `id` không tồn tại** | Hub vẫn thêm kết nối vào nhóm nhưng không bao giờ nhận được thông báo. | Hub không báo lỗi, kết nối nằm chờ trong nhóm `submission:{id}` và hết thời gian chờ (timeout). | Hub mỏng chỉ làm nhiệm vụ định tuyến nhóm, không truy vấn kiểm tra nghiệp vụ ở tầng dữ liệu. |
| **4** | **Đổi trạng thái trên máy chủ nhưng không gọi `SendAsync`** | Dữ liệu REST thay đổi nhưng máy khách SignalR không nhận được tin. | Gọi `GET /api/submissions/{id}` thấy trạng thái đã là `graded`, nhưng màn hình chờ SignalR đứng yên. | Cập nhật trạng thái dữ liệu và phát sự kiện thông báo là hai thao tác tách biệt nhau trên máy chủ. |
| **5** | **Máy khách gọi `GET` REST ngay sau khi nhận sự kiện `graded`** | Dữ liệu đọc lại từ REST khớp hoàn toàn với trạng thái SignalR vừa báo. | Dòng cuối in ra `Trang thai doc lai bang REST: graded`, trùng khớp 100% với thông báo SignalR. | Khóa `submissionsLock` đảm bảo tính nhất quán dữ liệu giữa luồng ghi của tác vụ nền và luồng đọc của HTTP API. |