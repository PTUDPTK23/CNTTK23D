# BÀI 3 – ỨNG DỤNG SIGNALR

**Sinh viên:** Nguyễn Xuân Hải  
**Mã sinh viên:** dtc245200551

## 1. Giới thiệu

Bài tập xây dựng ứng dụng giao tiếp thời gian thực sử dụng SignalR trong .NET 10.

Bài tập gồm hai phần:

- **PingSignalR:** Minh họa kết nối SignalR giữa Client và Server, gửi thông điệp Ping và nhận phản hồi Pong.
- **RealtimeSubmissionSystem:** Xây dựng hệ thống tiếp nhận bài nộp, theo dõi trạng thái xử lý và nhận thông báo thời gian thực qua SignalR.

## 2. Công nghệ sử dụng

- C# và .NET 10
- ASP.NET Core Web API
- SignalR
- REST API
- HTTP và JSON

## 3. Cấu trúc dự án

```text
Bai3_Nguyen_Xuan_Hai_dtc245200551/
├── PingSignalR/
│   └── src/
│       ├── Ping.Api/
│       └── Ping.Client/
├── RealtimeSubmissionSystem/
│   └── src/
│       ├── Submission.RealtimeApi/
│       └── Submission.RealtimeClient/
├── Screenshots/
└── README.md
```

## 4. Phần 1 – PingSignalR

### Chức năng

- Khởi chạy SignalR Server.
- Client kết nối đến Hub tại `/hubs/ping`.
- Client gửi thông điệp `Hello SignalR`.
- Server phản hồi thông qua sự kiện `Pong`.
- Client hỗ trợ tự động kết nối lại khi mất kết nối.

### Cách chạy

Mở Terminal thứ nhất:

```powershell
dotnet run --project .\PingSignalR\src\Ping.Api\Ping.Api.csproj
```

Mở Terminal thứ hai:

```powershell
dotnet run --project .\PingSignalR\src\Ping.Client\Ping.Client.csproj
```

Địa chỉ API mặc định:

```text
http://localhost:5000
```

Hub:

```text
/hubs/ping
```

## 5. Phần 2 – RealtimeSubmissionSystem

### Chức năng

- Kiểm tra trạng thái hoạt động của API.
- Lấy danh sách bài tập.
- Tạo lượt nộp bài tập.
- Đọc thông tin lượt nộp.
- Kiểm tra dữ liệu không hợp lệ.
- Kiểm tra bài tập không tồn tại.
- Theo dõi trạng thái bài nộp qua SignalR.
- Nhận thông báo khi trạng thái thay đổi.

### Cách chạy

Mở Terminal thứ nhất:

```powershell
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeApi\Submission.RealtimeApi.csproj --urls http://localhost:5001
```

Mở Terminal thứ hai:

```powershell
dotnet run --project .\RealtimeSubmissionSystem\src\Submission.RealtimeClient\Submission.RealtimeClient.csproj
```

Địa chỉ API:

```text
http://localhost:5001
```

Hub:

```text
/hubs/submissions
```

## 6. Kiểm thử API

Các tình huống kiểm thử được thực hiện trong file `requests.http`:

| STT | Nội dung kiểm thử | Kết quả mong đợi |
|---|---|---|
| 1 | Kiểm tra API health | 200 OK |
| 2 | Lấy danh sách bài tập | 200 OK |
| 3 | Tạo lượt nộp hợp lệ | 201 Created |
| 4 | Đọc lượt nộp vừa tạo | 200 OK |
| 5 | Gửi dữ liệu thiếu | 400 Bad Request |
| 6 | Gửi bài tập không tồn tại | 404 Not Found |
| 7 | Đọc mã Submission không tồn tại | 404 Not Found |
| 8 | SignalR negotiate đúng đường dẫn | 200 OK |
| 9 | SignalR negotiate sai đường dẫn | 404 Not Found |

## 7. Ảnh minh chứng

Các ảnh chụp màn hình kết quả thực hiện được lưu trong thư mục:

```text
Screenshots/
```

## 8. Kết luận

Qua bài tập, em đã thực hành xây dựng ứng dụng sử dụng SignalR trong .NET 10, thực hiện giao tiếp thời gian thực giữa Client và Server, kết hợp REST API để tiếp nhận và truy vấn dữ liệu.

Bài tập giúp củng cố kiến thức về ASP.NET Core Web API, SignalR, kết nối Client-Server và kiểm thử API.