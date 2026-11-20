# Bài 3: Giao tiếp thời gian thực với SignalR

**Môn học:** Phát triển ứng dụng phân tán  
**Cấu trúc thư mục nộp bài (`Bai3/`):**

* `anh/`: Thư mục chứa ảnh chụp màn hình kết quả các bước thực hành.
* `PingSignalR/`: Giải pháp mẫu làm quen SignalR (Hub, Caller, All, Group, vòng đời kết nối, xử lý lỗi).
* `RealtimeSubmissionSystem/`: Hệ thống EduSubmit tích hợp REST API và SignalR thời gian thực (`requests.http` gồm 9 ca kiểm tra).

\---

## Phần 1: Trả lời 3 câu hỏi đặt vấn đề đầu giờ

1. **Máy chủ có thể chủ động báo cho máy khách mà không cần máy khách hỏi không?**

   * **Trả lời:** Có, với điều kiện máy khách mở sẵn một kết nối lâu dài hai chiều tới máy chủ (sử dụng giao thức WebSocket thông qua thư viện SignalR). Khi có sự kiện mới, máy chủ chủ động đẩy thông điệp xuống thông qua `Clients.Group(...).SendAsync(...)` (hoặc sử dụng `IHubContext` nếu phát tin từ bên ngoài Hub như endpoint REST hay tác vụ chạy nền).
2. **Đã có REST rồi, vì sao còn cần thêm SignalR? Hai thứ chia việc thế nào?**

   * **Trả lời:** Giao thức HTTP/REST thông thường hoạt động theo mô hình yêu cầu – phản hồi (chỉ máy khách được mở lời hỏi, máy chủ không tự gọi ngược lại được). Nếu dùng REST để hỏi lặp lại liên tục (Polling) sẽ gây lãng phí băng thông với nhiều yêu cầu thừa hoặc tạo độ trễ.
   * **Cách chia việc:** **REST** dùng để khởi tạo và đọc trạng thái tài nguyên với mã trạng thái rõ ràng (`POST` tạo lượt nộp $\\rightarrow$ `201 Created`, `GET` đọc chi tiết $\\rightarrow$ `200 OK`). **SignalR** bổ sung kênh đẩy thông báo tức thì các thay đổi trạng thái phát sinh sau yêu cầu ban đầu (`received` $\\rightarrow$ `processing` $\\rightarrow$ `graded`). SignalR không thay thế REST.
3. **Nếu đứt mạng đúng lúc máy chủ báo "đã chấm xong" (`graded`) thì sao?**

   * **Trả lời:** Thông điệp đẩy qua SignalR tại thời điểm rớt mạng sẽ bị mất hẳn vì SignalR không lưu trữ tin nhắn chờ gửi lại. Khi có mạng trở lại, `WithAutomaticReconnect()` tự động kết nối lại nhưng máy chủ sẽ cấp một `ConnectionId` mới (mất tư cách thành viên nhóm cũ). Do đó, nguyên tắc thiết kế bắt buộc là **REST đóng vai trò nguồn sự thật**: sau khi nối lại hoặc kết thúc luồng chờ, máy khách phải gọi lại `GET /api/submissions/{id}` để chốt trạng thái cuối cùng chính xác nhất.

\---

## Phần 2: Thực hành cơ bản `PingSignalR` (Cổng `5200`)

### Bước 1: Tạo solution, hai project và thêm gói SignalR Client

* **Thực hiện:** Tạo solution `PingSignalR`, hai project `Ping.Api` (web) và `Ping.Client` (console), cài đặt gói `Microsoft.AspNetCore.SignalR.Client` phiên bản `10.0.15` và biên dịch thử.
* **Kết quả:** `Build succeeded. 0 Warning(s) 0 Error(s)`.

> \*\*\[CHÚ THÍCH CHÈN ẢNH 1]:\*\* Chụp màn hình Terminal chạy lệnh `dotnet build` báo `Build succeeded, 0 Error(s)` (thấy rõ đường dẫn thư mục `Bai3/PingSignalR`), lưu vào `anh/buoc1-build.png`.

!\[Bước 1 - Build solution](anh/buoc1-build.png)

\---

### Bước 2: Cấu hình Hub và kiểm tra bắt tay `negotiate`

* **Thực hiện:** Đăng ký `AddSignalR()` và ánh xạ `MapHub<PingHub>("/hubs/ping")` trên máy chủ. Dùng `curl.exe` kiểm tra endpoint gốc và bước bắt tay `/negotiate`.
* **Kết quả \& Giải thích:** Gọi `POST /hubs/ping/negotiate?negotiateVersion=1` trả về JSON chứa `connectionId` và danh sách 3 cơ chế truyền tải dự phòng theo thứ tự ưu tiên: `WebSockets`, `ServerSentEvents`, `LongPolling`. Gọi trực tiếp `GET /hubs/ping` trả về `400 Bad Request` (`Connection ID required`).

> \*\*\[CHÚ THÍCH CHÈN ẢNH 2]:\*\* Chụp màn hình Terminal chạy 3 lệnh `curl.exe` kiểm tra `/`, `/hubs/ping/negotiate` (thấy rõ `connectionId`) và `/hubs/ping`, lưu vào `anh/buoc2-negotiate.png`.

!\[Bước 2 - Negotiate](anh/buoc2-negotiate.png)

\---

### Bước 3 \& 4: Viết phương thức `Echo` (`Clients.Caller`) và máy khách `HubConnection`

* **Thực hiện:** Thêm phương thức `Echo` trên `PingHub` gửi trả thông điệp `"EchoResponse"` qua `Clients.Caller`. Phía `Ping.Client` xây dựng `HubConnectionBuilder`, đăng ký `On<string>("EchoResponse", ...)`, gọi `StartAsync()` và `InvokeAsync("Echo", ...)`.
* **Kết quả \& Giải thích:** Máy khách kết nối thành công và nhận phản hồi riêng từ máy chủ: `\[Echo] Echo: Xin chao SignalR tu An`.

> \*\*\[CHÚ THÍCH CHÈN ẢNH 3]:\*\* Chụp màn hình 2 cửa sổ Terminal chạy song song: Terminal 1 máy chủ đang lắng nghe cổng `5200`, Terminal 2 máy khách in `\[Echo] Echo: Xin chao SignalR tu An`, lưu vào `anh/buoc4-echo.png`.

!\[Bước 4 - Echo](anh/buoc4-echo.png)

\---

### Bước 5: Phát rộng (`Clients.All`) và Nhóm (`Clients.Group`)

* **Thực hiện:** Bổ sung `Broadcast` (gửi cho `Clients.All`), `JoinGroup` và `SendToGroup` (gửi cho `Clients.Group("lop-hoc-01")`). Chạy đồng thời 2 máy khách `An` (chạy trước) và `Binh` (chạy sau).
* **Kết quả \& Giải thích:** Cửa sổ của `An` nhận được thông báo tham gia nhóm, tin nhắn nhóm và tin phát rộng từ `Binh`, nhưng **không** nhận được dòng `\[Echo]` của `Binh` vì `Clients.Caller` chỉ phản hồi cho đúng kết nối đã gọi hàm.

> \*\*\[CHÚ THÍCH CHÈN ẢNH 4]:\*\* Chụp màn hình 3 cửa sổ Terminal (Máy chủ, An, Bình) cho thấy cửa sổ An nhận `\[Group]` và `\[Broadcast]` của Bình nhưng không lẫn `\[Echo]` của Bình, lưu vào `anh/buoc5-nhom.png`.

!\[Bước 5 - Nhóm và Phát rộng](anh/buoc5-nhom.png)

\---

### Bước 6: Vòng đời kết nối và tự động kết nối lại (`WithAutomaticReconnect`)

* **Thực hiện:** Ghi đè `OnConnectedAsync` và `OnDisconnectedAsync` trên `PingHub`. Phía máy khách lắng nghe sự kiện `Reconnecting`, `Reconnected`, `Closed`. Thử tắt máy chủ (`Ctrl + C`) khi `An` đang mở rồi bật lại ngay.
* **Kết quả \& Giải thích:** Khi máy chủ tắt, máy khách lập tức báo `\[Lifecycle] Dang ket noi lai...`. Khi máy chủ bật lại, máy khách tự nối lại mà không cần khởi động lại chương trình, in `\[Lifecycle] Da ket noi lai: ...` kèm một mã `ConnectionId` hoàn toàn mới.

> \*\*\[CHÚ THÍCH CHÈN ẢNH 5]:\*\* Chụp màn hình Terminal máy khách in đủ 2 dòng `\[Lifecycle] Dang ket noi lai...` và `\[Lifecycle] Da ket noi lai: <ID mới>`, lưu vào `anh/buoc6-noilai.png`.

!\[Bước 6 - Tự động kết nối lại](anh/buoc6-noilai.png)

\---

### Bước 7: Ba thí nghiệm lỗi điển hình trên `PingSignalR`

|STT|Thí nghiệm cố ý|Kết quả quan sát được|Giải thích nguyên nhân \& Bài học|
|:-:|-|-|-|
|**1**|Chạy `Ping.Client` khi `Ping.Api` chưa chạy|Chương trình dừng ngay và ném ngoại lệ `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5200)`|Hàm `StartAsync()` báo lỗi ngay nếu lần kết nối đầu tiên thất bại. `WithAutomaticReconnect()` chỉ cứu kết nối **đã mở thành công** rồi mới bị rớt.|
|**2**|Sửa đường dẫn Hub ở client thành `/hub/ping` (thiếu chữ `s`)|Ném ngoại lệ `HttpRequestException: Response status code does not indicate success: 404 (Not Found)`|Lỗi xảy ra ngay bước bắt tay `negotiate`. Đường dẫn trong `WithUrl` phải khớp chính xác từng ký tự với `MapHub`.|
|**3**|Sửa tên sự kiện nhận ở client thành `"EchoRespone"` (sai chính tả)|Không báo lỗi biên dịch hay ngoại lệ lúc chạy, nhưng màn hình **không in dòng `\[Echo]`**|Giao tiếp đẩy từ server xuống là bất đồng bộ theo tên sự kiện. Sai tên chiều xuống sẽ bị bỏ qua trong im lặng.|

> \*\*\[CHÚ THÍCH CHÈN ẢNH 6]:\*\* Chụp màn hình kết quả 3 lỗi cố ý ở Bước 7 và lưu thành 3 tệp: `anh/buoc7-loi1.png`, `anh/buoc7-loi2.png`, `anh/buoc7-loi3.png`.

!\[Bước 7 - Lỗi 1: Server chưa chạy](anh/buoc7-loi1.png)
!\[Bước 7 - Lỗi 2: Sai đường dẫn Hub 404](anh/buoc7-loi2.png)
!\[Bước 7 - Lỗi 3: Sai tên sự kiện im lặng](anh/buoc7-loi3.png)

\---

## Phần 3: Hệ thống EduSubmit Realtime (`RealtimeSubmissionSystem` - Cổng `5000`)

### 1\. Thiết kế luồng kết hợp REST + SignalR

* **Kênh REST:** `POST /api/assignments/{id}/submissions` nhận bài nộp, khóa danh sách bằng `lock (submissionsLock)`, lưu trạng thái ban đầu `"received"`, kích hoạt tác vụ chấm nền bất đồng bộ và trả ngay mã `201 Created`.
* **Kênh SignalR (`/hubs/submissions`):** Máy khách gọi `WatchSubmission(submissionId)` để tham gia nhóm riêng `submission:{id}`. Tác vụ chấm nền sử dụng `IHubContext<SubmissionHub>` đẩy sự kiện `"SubmissionStatusChanged"` kèm gói tin tối thiểu `SubmissionStatusMessage(SubmissionId, Status, UpdatedAt)` khi trạng thái đổi từ `received` $\\rightarrow$ `processing` (sau 1s) $\\rightarrow$ `graded` (sau 2s tiếp theo).
* **Chốt dữ liệu:** Sau khi nhận thông báo `graded` (hoặc hết thời gian chờ 10s), máy khách gọi lại `GET /api/submissions/{id}` để đọc trạng thái cuối cùng từ nguồn sự thật REST.

### 2\. Kết quả chạy thực tế trên máy khách (`Submission.RealtimeClient`)

Chạy lệnh: `dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV001`

```text
Da ket noi SignalR.
Chon bai tap: 1 - Bai tap SignalR
Da tao luot nop: 809b770d-7874-4ef6-804f-bacadf084ad1
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: 809b770d-7874-4ef6-804f-bacadf084ad1 -> processing luc 02:50:49
Thong bao SignalR: 809b770d-7874-4ef6-804f-bacadf084ad1 -> graded luc 02:50:51
Trang thai doc lai bang REST: graded
```

> \*\*\[CHÚ THÍCH CHÈN ẢNH 7]:\*\* Chụp màn hình 2 cửa sổ Terminal của EduSubmit: Terminal trái máy chủ in `\[Hub] ... theo doi` và 2 dòng `\[Push]`; Terminal phải máy khách in đủ 8 dòng kết quả tới `graded`, lưu vào `anh/edusubmit-client.png`.

!\[EduSubmit - Kết quả chạy máy chủ và máy khách](anh/edusubmit-client.png)

\---

### 3\. Bảng kết quả 5 thí nghiệm phân tán (Bước 7)

|STT|Tình huống thí nghiệm|Dự đoán ban đầu|Kết quả chạy thật|Giải thích hiện tượng|
|:-:|-|-|-|-|
|**1**|Chạy đồng thời 2 máy khách `SV001` và `SV002`|Mỗi sinh viên chỉ nhận thông báo của bài mình nộp|Mỗi cửa sổ chỉ in `processing` và `graded` của đúng mã `SubmissionId` mà máy khách đó vừa tạo, không bị lẫn tin của người khác|Nhờ cơ chế chia nhóm theo mã lượt nộp `Clients.Group("submission:{id}")`, máy chủ chỉ đẩy tin tới đúng kết nối đã đăng ký theo dõi lượt nộp đó.|
|**2**|Chạy máy khách khi máy chủ `RealtimeApi` chưa bật|Máy khách báo lỗi không kết nối được và dừng|Chương trình dừng ngay lập tức và ném ngoại lệ `HttpRequestException: Connection refused (localhost:5000)`|Hàm `StartAsync()` không tự thử lại ở lần kết nối đầu tiên; `WithAutomaticReconnect()` chỉ hoạt động sau khi kết nối đã thiết lập thành công.|
|**3**|Sửa đường dẫn Hub ở client thành `/hub/submissions` (thiếu `s`)|Báo lỗi không tìm thấy đường dẫn (404)|Ném ngoại lệ `HttpRequestException: Response status code does not indicate success: 404 (Not Found)`|Lỗi xảy ra ngay ở bước bắt tay `negotiate` do URL phía client không khớp với `MapHub<SubmissionHub>("/hubs/submissions")` phía server.|
|**4**|Sửa tên sự kiện ở client thành `On("SubmissionStatusChange")` (thiếu `d`)|Không nhận được thông báo SignalR|Không báo lỗi biên dịch hay runtime; máy khách chờ hết 10 giây rồi in `Qua thoi gian cho thong bao graded.` và `Trang thai doc lai bang REST: graded`|Sai tên sự kiện chiều đẩy xuống khiến client bỏ qua thông điệp trong im lặng. Nhờ bước gọi lại REST cuối cùng nên vẫn lấy được trạng thái `graded` chính xác.|
|**5**|Chờ 4 giây sau khi `POST` rồi mới gọi `WatchSubmission`|Có thể bị lỡ thông báo do chấm bài mất 3 giây|Không nhận được thông báo SignalR nào, hết 10 giây in `Qua thoi gian cho thong bao graded.`, nhưng dòng cuối vẫn in `Trang thai doc lai bang REST: graded`|Quá trình chấm nền hoàn tất sau 3 giây nên lúc vào nhóm ở giây thứ 4 thì server đã đẩy tin xong. SignalR không lưu tin cũ, chứng minh REST luôn là nguồn sự thật đáng tin cậy.|

\---

### 4\. Kiểm thử API \& SignalR với `requests.http` (Bước 8)

Tệp `RealtimeSubmissionSystem/requests.http` bao gồm đầy đủ **9 ca kiểm tra**:

1. `GET /` $\\rightarrow$ `200 OK`
2. `GET /api/assignments` $\\rightarrow$ `200 OK`
3. `POST /api/assignments/1/submissions` (dữ liệu hợp lệ) $\\rightarrow$ `201 Created` + header `Location`, trạng thái `"received"`
4. `GET /api/submissions/{{id}}` $\\rightarrow$ `200 OK`, quan sát trạng thái chuyển dần `received` $\\rightarrow$ `processing` $\\rightarrow$ `graded`
5. `POST /api/assignments/1/submissions` (thiếu `studentCode` / `repositoryUrl`) $\\rightarrow$ `400 Bad Request` (`INVALID\_SUBMISSION`)
6. `POST /api/assignments/99/submissions` (bài tập không tồn tại) $\\rightarrow$ `404 Not Found` (`ASSIGNMENT\_NOT\_FOUND`)
7. `GET /api/submissions/00000000-0000-0000-0000-000000000000` $\\rightarrow$ `404 Not Found` (`SUBMISSION\_NOT\_FOUND`)
8. `POST /hubs/submissions/negotiate?negotiateVersion=1` $\\rightarrow$ `200 OK` kèm `connectionId`
9. `POST /hub/submissions/negotiate?negotiateVersion=1` (sai đường dẫn hub) $\\rightarrow$ `404 Not Found`

> \*\*\[CHÚ THÍCH CHÈN ẢNH 8]:\*\* Chụp màn hình kết quả chạy các ca kiểm tra trong `requests.http` (thấy rõ mã trả về `201 Created`, `400 Bad Request`, `404 Not Found` và `200 OK` của `negotiate`), lưu vào `anh/edusubmit-requests.png`.

!\[EduSubmit - Kết quả kiểm thử requests.http](anh/edusubmit-requests.png)

