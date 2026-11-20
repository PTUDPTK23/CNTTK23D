# Bài 3 — SignalR

**Sinh viên:** Nguyễn Danh Hùng

**MSSV:** DTC225201515

Hai solution theo tài liệu Bài 3:

- `PingSignalR`: ví dụ Echo, broadcast, group và vòng đời kết nối.
- `RealtimeSubmissionSystem`: REST tạo/đọc lượt nộp và SignalR đẩy trạng thái tới nhóm riêng của lượt nộp.

## Yêu cầu

- .NET SDK 10
- Kết nối NuGet khi restore gói `Microsoft.AspNetCore.SignalR.Client` phiên bản `10.0.15`

## Build

Chạy trong thư mục `Bai3`:

```powershell
dotnet build .\PingSignalR\PingSignalR.slnx
dotnet build .\RealtimeSubmissionSystem\RealtimeSubmissionSystem.slnx
```

## Chạy PingSignalR

Terminal 1:

```powershell
dotnet run --project .\PingSignalR\src\Ping.Api --urls http://localhost:5200
```

Terminal 2 và 3:

```powershell
dotnet run --project .\PingSignalR\src\Ping.Client -- http://localhost:5200 An
dotnet run --project .\PingSignalR\src\Ping.Client -- http://localhost:5200 Binh
```

An và Bình nhận thông điệp nhóm và broadcast của nhau. `EchoResponse` chỉ về máy khách vừa gọi. Nếu máy chủ khởi động lại, client tự thử nối lại; ConnectionId đổi và client gọi `JoinGroup` lần nữa.

## Chạy EduSubmit realtime

Terminal 1:

```powershell
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeApi --urls http://localhost:5000
```

Terminal 2:

```powershell
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeClient -- http://localhost:5000 SV001
```

Máy khách đọc bài tập bằng REST, tạo lượt nộp, gọi `WatchSubmission`, chờ `processing` rồi `graded`, sau đó đọc lại trạng thái bằng REST. REST là nguồn dữ liệu cuối cùng; SignalR chỉ báo thay đổi và tin có thể bị lỡ khi mất kết nối.

## Bài tập mở rộng

Để nhận trạng thái `failed` khi URL không bắt đầu bằng `https://`:

```powershell
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeClient -- http://localhost:5000 SV001 --fail
```

Để theo dõi hai lượt nộp đồng thời trên cùng một kết nối SignalR:

```powershell
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeClient -- http://localhost:5000 SV001 --two
```

Client giữ một `TaskCompletionSource` cho mỗi GUID trong `ConcurrentDictionary`. Lượt đầu dùng URL HTTPS và kết thúc `graded`; lượt thứ hai dùng URL HTTP và kết thúc `failed`. Mỗi thông báo được ghép theo `SubmissionId`, nên trạng thái không bị trộn.

Các cờ dưới đây tái hiện thí nghiệm lỗi trong tài liệu:

```powershell
# Ping: đường dẫn hub sai hoặc tên EchoResponse sai
dotnet run --project .\PingSignalR\src\Ping.Client -- http://localhost:5200 An --wrong-hub
dotnet run --project .\PingSignalR\src\Ping.Client -- http://localhost:5200 An --wrong-event

# EduSubmit: đường dẫn hub sai, tên sự kiện sai, hoặc Watch trễ 4 giây
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeClient -- http://localhost:5000 SV001 --wrong-hub
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeClient -- http://localhost:5000 SV001 --bad-event
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeClient -- http://localhost:5000 SV001 --watch-late
```

## requests.http

`RealtimeSubmissionSystem/requests.http` có 9 ca của tài liệu: API status, danh sách, tạo và đọc lượt nộp, thiếu dữ liệu, mã bài không tồn tại, GUID không tồn tại, negotiate đúng và sai đường dẫn hub.

## Trả lời câu hỏi đầu giờ

1. **Máy chủ có thể chủ động báo không?** Có. Máy khách mở kết nối SignalR trước; hub gửi bằng `Clients.Group(...).SendAsync`. Tác vụ ngoài hub dùng `IHubContext`.
2. **REST và SignalR chia việc thế nào?** REST tạo và đọc tài nguyên với mã HTTP rõ ràng. SignalR đẩy các thay đổi phát sinh sau đó. SignalR không thay REST.
3. **Mất mạng đúng lúc báo `graded` thì sao?** Thông báo có thể mất. Sau khi nối lại, client theo dõi lại lượt nộp rồi gọi `GET /api/submissions/{id}` để lấy trạng thái hiện tại.

## Kết quả đã chạy

Ngày 20/11/2026, đã build hai solution: cả bốn project đều build thành công, 0 lỗi. NuGet in cảnh báo `NU1900` khi môi trường không đọc được dữ liệu kiểm tra lỗ hổng; gói SignalR Client vẫn restore được và không có lỗi biên dịch.

Các kiểm tra HTTP thực tế trả về: `GET /` 200, `GET /api/assignments` 200, POST hợp lệ 201, POST thiếu trường 400, POST assignment 999 là 404, GET GUID không tồn tại 404, negotiate đúng 200 và negotiate sai đường dẫn 404.

PingSignalR negotiate trả `connectionId` cùng ba transport `WebSockets`, `ServerSentEvents`, `LongPolling`. Khi chạy An trước Bình, An nhận `GroupMessage` và `BroadcastMessage` của Bình, không nhận `EchoResponse` của Bình.

EduSubmit nhận hai thông báo `processing` rồi `graded`; GET cuối trả `graded`. Chế độ `--two` nhận hai lượt đồng thời trên một kết nối, một lượt `graded` và một lượt `failed`; mỗi trạng thái khớp GUID và GET REST tương ứng.

## Nhật ký thí nghiệm

### PingSignalR — Bước 7

| Thao tác | Kết quả quan sát | Giải thích |
|---|---|---|
| Tắt API rồi chạy client | Đã chạy: `HttpRequestException`, connection refused | `WithAutomaticReconnect` chỉ thử lại sau khi kết nối đã từng mở. |
| Đổi URL hub thành `/hub/ping` | Đã chạy `--wrong-hub`: HTTP 404 ở negotiate | Đường dẫn phải khớp `MapHub("/hubs/ping")`. |
| Đổi `EchoResponse` thành `EchoRespone` ở client | Đã chạy `--wrong-event`: không lỗi, không in `[Echo]` | Sai tên thông điệp chiều xuống bị bỏ qua im lặng. |

### EduSubmit — Bước 7

| Thí nghiệm | Dự đoán | Kết quả quan sát | Bài học |
|---|---|---|---|
| Hai client SV001 và SV002 cùng chạy | Đã chạy đồng thời: mỗi client chỉ nhận GUID của lượt mình | Mỗi lượt có nhóm `submission:{id}` riêng | Nhóm giới hạn người nhận và tránh lộ trạng thái lượt khác. |
| Chạy client khi API tắt | Đã chạy: `HttpRequestException`, connection refused | Lần nối đầu không được tự thử lại | Tự kết nối lại không thay thế lần kết nối đầu tiên. |
| Dùng `/hub/submissions` | Đã chạy `--wrong-hub`: negotiate trả 404 | `WithUrl` phải trùng `MapHub`. |
| Đổi `SubmissionStatusChanged` thành `SubmissionStatusChange` | Đã chạy `--bad-event`: hết 10 giây, SignalR timeout; REST trả `graded` | Tên sự kiện gửi và tên sự kiện nghe phải trùng từng ký tự. |
| Chờ 4 giây rồi mới `WatchSubmission` | Đã chạy `--watch-late`: không nhận tin; REST trả `graded` | SignalR không lưu tin; REST là nguồn sự thật. |

## Giới hạn của bản thực hành

Chấm bài được mô phỏng bằng `Task.Delay`; danh sách lượt nộp và nhóm SignalR nằm trong bộ nhớ của một tiến trình. Bản triển khai thật cần hàng đợi/worker, lưu trữ bền vững, xác thực quyền vào nhóm và backplane khi chạy nhiều máy chủ.
