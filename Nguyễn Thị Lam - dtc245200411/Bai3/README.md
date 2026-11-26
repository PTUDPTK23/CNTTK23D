# Báo cáo thực hành Bài 3: Giao tiếp thời gian thực với SignalR

## I. Trả lời 3 câu hỏi đầu giờ

1. **Máy chủ có thể chủ động báo cho máy khách mà không cần máy khách hỏi không?**
   * **Trả lời:** Có, nếu máy khách mở sẵn một kết nối hai chiều lâu dài (như WebSocket thông qua SignalR)[cite: 67]. Khi có sự kiện xảy ra, máy chủ chủ động đẩy thông điệp về cho máy khách bằng `Clients.Group(...).SendAsync(...)` (hoặc dùng `IHubContext` nếu gửi từ bên ngoài Hub, ví dụ từ tác vụ nền)[cite: 67].

2. **Đã có REST rồi, vì sao còn cần thêm SignalR? Hai thứ chia việc thế nào?**
   * **Trả lời:** Vì REST hoạt động theo cơ chế hỏi - đáp (chỉ máy khách được mở lời), nếu dùng REST để cập nhật trạng thái liên tục sẽ phải gọi lặp lại định kỳ (polling) gây tốn tài nguyên và có độ trễ[cite: 8, 9]. Hai công nghệ chia việc rõ ràng[cite: 18]:
     * **REST:** Dùng để tạo tài nguyên (`POST`) và đọc trạng thái hiện tại (`GET` - đóng vai trò là nguồn sự thật)[cite: 18, 67].
     * **SignalR:** Chỉ dùng để đẩy các thông báo thay đổi trạng thái phát sinh sau yêu cầu ban đầu (`processing`, `graded`), không thay thế REST[cite: 18, 67].

3. **Nếu đứt mạng đúng lúc máy chủ báo "đã chấm xong" (`graded`) thì sao?**
   * **Trả lời:** Thông điệp đó sẽ bị mất vì SignalR không lưu trữ tin nhắn chờ gửi lại[cite: 41, 67]. Khi có mạng trở lại, `WithAutomaticReconnect()` sẽ tự động nối lại với một `ConnectionId` mới; lúc này máy khách cần đăng ký vào nhóm lại và gọi `GET /api/submissions/{id}` qua REST để lấy trạng thái cuối cùng chính xác nhất[cite: 41, 67].

---

## II. Minh chứng chạy chương trình

### 1. Phần 2: PingSignalR
* **Bước 2 (Negotiate):** ![Negotiate Ping](images/buoc2-negotiate.png)
* **Bước 4-5 (Echo, Group, Broadcast giữa An và Bình):** ![An va Binh](images/buoc5-nhom.png)
* **Bước 6 (Tự động kết nối lại):** ![Ket noi lai](images/buoc6-noilai.png)

### 2. Phần 3: EduSubmit Realtime
* **Bước 6 (Kết quả chạy máy khách đủ 8 dòng):** ![EduSubmit Client](images/edusubmit-client.png)
* **Bước 8 (Kết quả chạy requests.http):** ![Requests HTTP](images/edusubmit-requests.png)

---

## III. Kết quả 5 thí nghiệm phân tán (Bước 7 - EduSubmit)

| TT | Thí nghiệm | Dự đoán | Kết quả chạy thật | Giải thích hiện tượng |
| :--- | :--- | :--- | :--- | :--- |
| **1** | Chạy 2 máy khách `SV001` và `SV002` cùng lúc | Mỗi sinh viên chỉ nhận thông báo bài của mình | Mỗi cửa sổ chỉ in đúng mã lượt nộp của sinh viên đó, không bị lẫn | Máy chủ tách người nhận theo từng nhóm riêng biệt `submission:{id}` qua `Clients.Group`[cite: 61]. |
| **2** | Chạy máy khách khi máy chủ chưa bật | Báo lỗi không kết nối được | Báo lỗi `HttpRequestException: Connection refused (localhost:5000)` và dừng ngay | `StartAsync()` ném lỗi ngay ở lần kết nối đầu tiên; `WithAutomaticReconnect` chỉ cứu các kết nối đã mở thành công trước đó[cite: 42, 61]. |
| **3** | Sửa `WithUrl` thành `/hub/submissions` (thiếu `s`) | Lỗi không tìm thấy đường dẫn (404) | Báo lỗi `HttpRequestException: 404 (Not Found)` | Lỗi xảy ra ngay ở bước bắt tay `negotiate` do URL phía máy khách không khớp với tuyến đã `MapHub` ở máy chủ[cite: 61]. |
| **4** | Sửa tên sự kiện `On("SubmissionStatusChanged")` thành `On("SubmissionStatusChange")` | Máy khách không nhận được thông báo | Không báo lỗi gì; sau 10 giây in `Qua thoi gian cho thong bao graded.` | Sai tên thông điệp ở chiều máy chủ gửi xuống sẽ bị bỏ qua trong im lặng (không văng ngoại lệ)[cite: 61]. |
| **5** | Chờ 4 giây (`Task.Delay(4000)`) rồi mới gọi `WatchSubmission` | Bị lỡ thông báo SignalR nhưng REST vẫn đọc đúng | Không in dòng thông báo SignalR nào, nhưng dòng cuối `GET` vẫn in `graded` | Máy chủ chấm xong ở giây thứ 3 nên vào nhóm lúc giây thứ 4 bị lỡ tin; gọi lại `GET` qua REST giúp lấy đúng trạng thái cuối[cite: 61]. |