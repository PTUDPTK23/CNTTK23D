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

### 1. Máy chủ có thể chủ động báo không?

Có. Máy khách mở kết nối SignalR lâu dài trước khi có sự kiện. Khi trạng thái đổi, máy chủ gọi `IHubContext` để gửi đến các kết nối trong nhóm. Trong bài này, `submission:{id}` chỉ chứa máy khách đang theo dõi đúng lượt nộp đó.

### 2. REST và SignalR chia việc thế nào?

REST tạo lượt nộp và đọc trạng thái hiện tại bằng các request có phản hồi rõ như `201`, `400` và `404`. SignalR đẩy thông báo thay đổi sau request ban đầu. Thông báo realtime giúp máy khách biết sớm, nhưng không thay thế dữ liệu REST: khi kết nối lại hoặc nghi ngờ bỏ lỡ sự kiện, máy khách GET lại trạng thái chuẩn từ REST.

### 3. Nếu đứt mạng đúng lúc báo `graded` thì sao?

Máy khách có thể bỏ lỡ sự kiện vì SignalR không lưu lịch sử để phát lại. `WithAutomaticReconnect` mở kết nối mới; mã `Reconnected` phải gọi lại `WatchSubmission` vì kết nối mới có `ConnectionId` và chưa ở trong nhóm. Máy khách vẫn GET lượt nộp qua REST để chốt trạng thái cuối, kể cả khi không nhận được push.

## Năm thí nghiệm phân tán

Ghi dự đoán trước khi chạy; kết quả quan sát được đối chiếu với hành vi thực tế của hai ứng dụng.

| Thí nghiệm | Dự đoán | Kết quả quan sát và bài học |
| --- | --- | --- |
| Hai máy khách SV001 và SV002 chạy cùng lúc | Mỗi máy chỉ nhận sự kiện của id nó đã theo dõi. | Cả hai nhận `processing` rồi `graded` cho GUID riêng của mình; không thấy GUID của máy kia. Nhóm phân tách đúng người nhận. |
| Chạy máy khách khi máy chủ tắt | Lần kết nối đầu thất bại; `WithAutomaticReconnect` không thể thay thế việc khởi động máy chủ. | GET `/api/assignments` bị `Connection refused` trước khi client bắt đầu theo dõi. Cần khởi động lại client sau khi API sẵn sàng. |
| Đổi `WithUrl` sang `/hub/submissions` | Đường dẫn không khớp `MapHub`, negotiate thất bại. | POST negotiate trả `404`; hub URL phải khớp chính xác `/hubs/submissions`. |
| Đổi tên event lắng nghe thành `SubmissionStatusChange` | Không có lỗi giao thức, nhưng callback không chạy. | Không có dòng thông báo SignalR; sau thời gian chờ, REST GET vẫn trả `graded`. Tên event ở `On<T>` và `SendAsync` phải giống từng ký tự. |
| Gọi `WatchSubmission` sau 4 giây | Máy khách vào nhóm sau khi chấm xong và sẽ không nhận sự kiện cũ. | Không có push nào được phát lại; REST GET trả `graded`. Vào nhóm sau POST và luôn dùng REST để đối chiếu. |

Thí nghiệm tên event sai dùng cờ `--event-name-mismatch`; thí nghiệm theo dõi muộn dùng `--watch-delay=4`. Đây là cờ phục vụ tái hiện lỗi, không đổi hành vi chạy mặc định.

## Giới hạn

- Dữ liệu và nhóm chỉ nằm trong bộ nhớ một tiến trình; restart sẽ mất lượt nộp, còn nhiều replica cần kho chia sẻ và SignalR backplane.
- `processing` và kết quả cuối đang được mô phỏng bằng `Task.Delay`; đây chưa phải worker chấm bài thật.
- Hub chỉ phát trạng thái, không xác thực danh tính hay phân quyền vào nhóm; hệ thống thật cần kiểm tra quyền trước khi cho theo dõi id.

