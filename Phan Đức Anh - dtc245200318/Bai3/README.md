# Bài 3 - SignalR

**Sinh viên:** Phan Đức Anh  
**MSSV:** DTC245200318  
**Lớp:** CNTT K23D  
**Học phần:** Phát triển ứng dụng phân tán

Bài gồm hai ứng dụng độc lập. `PingSignal` kiểm tra lời gọi hub hai chiều. `RealtimeSubmissionSystem` dùng REST để tạo/đọc lượt nộp và SignalR để đẩy các thay đổi `received -> processing -> graded` (hoặc `failed`) đến đúng nhóm `submission:{id}`.

## Cấu trúc

```text
Bai3/
├── README.md
├── PingSignal/
│   ├── PingSignal.slnx
│   └── src/Ping.Api, src/Ping.Client
└── RealtimeSubmissionSystem/
    ├── RealtimeSubmissionSystem.slnx
    ├── requests.http
    └── src/Submission.RealtimeApi, src/Submission.RealtimeClient
```

## Yêu cầu và build

Cần .NET 10 SDK và kết nối NuGet lần đầu để tải `Microsoft.AspNetCore.SignalR.Client` 10.0.15. Từ thư mục `Bai3`:

```powershell
dotnet build PingSignal/PingSignal.slnx
dotnet build RealtimeSubmissionSystem/RealtimeSubmissionSystem.slnx
```

## Chạy PingSignal

Terminal 1:

```powershell
dotnet run --project PingSignal/src/Ping.Api --urls http://localhost:5200
```

Terminal 2:

```powershell
dotnet run --project PingSignal/src/Ping.Client -- http://localhost:5200
```

Client mở kết nối `/hubs/ping`, gọi hub method `Echo` và nhận chuỗi phản hồi. Hub chỉ giữ logic giao tiếp, không lưu trạng thái.

## Chạy hệ thống EduSubmit realtime

Terminal 1:

```powershell
dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeApi --urls http://localhost:5000
```

Terminal 2:

```powershell
dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeClient -- http://localhost:5000
```

Kết quả máy khách có 8 dòng, gồm chọn bài, mở SignalR, tạo lượt nộp, trạng thái REST ban đầu, hai thông báo push và trạng thái REST đọc lại. `received` được trả ngay sau POST; tác vụ nền đổi sang `processing` sau 1 giây và kết thúc sau 3 giây. URL HTTPS được đánh dấu `graded`; URL không phải HTTPS kết thúc ở `failed`.

Ví dụ kết quả đã chạy:

```text
Chọn bài tập: 1 - Bai tap SignalR
Đã kết nối SignalR.
Đã tạo lượt nộp: b9d53a30-0188-4bc4-9d1d-b887adf5c482
Trạng thái REST ban đầu: received
Đang theo dõi trạng thái bằng SignalR...
Thông báo SignalR: ... -> processing
Thông báo SignalR: ... -> graded
Trạng thái đọc lại bằng REST: graded
```

### API và thiết kế

| Request | Kết quả |
| --- | --- |
| `GET /` | `200`, trạng thái API |
| `GET /api/assignments` | `200`, danh sách bài tập |
| `POST /api/assignments/1/submissions` hợp lệ | `201` + `Location`, trạng thái `received` |
| POST thiếu `studentCode` hoặc `repositoryUrl` | `400` + `ApiError` |
| POST vào bài tập không tồn tại | `404` + `ApiError` |
| `GET /api/submissions/{id}` có thật | `200`, trạng thái mới nhất |
| GET GUID chưa tồn tại | `404` + `ApiError` |
| `POST /hubs/submissions/negotiate?negotiateVersion=1` | `200`, SignalR connection id |
| Negotiate với đường dẫn hub sai | `404` |

Chi tiết các request có thể chạy trong REST Client của VS Code ở [requests.http](RealtimeSubmissionSystem/requests.http). API là nguồn dữ liệu chuẩn trong bộ nhớ của một tiến trình. Hub chỉ thêm kết nối vào nhóm, không chứa nghiệp vụ chấm. Khi chạy nhiều bản sao API, nhóm và dữ liệu trong bộ nhớ cần backplane/kho dùng chung; nội dung này được ghi ở giới hạn của bài thực hành.

## Ghi nhận ba câu hỏi đầu giờ

<!-- Câu trả lời và thí nghiệm được ghi ở các phần tiếp theo của README. -->

