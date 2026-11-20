# Bài 3 — Giao tiếp thời gian thực với SignalR

Repo này hoàn thành hai solution trong slide: `PingSignalR` và `RealtimeSubmissionSystem`. Mã ví dụ dùng .NET 10, cổng Ping `5200`, cổng EduSubmit `5000`.

## Nội dung bài học

- **Polling**: máy khách hỏi lặp lại; dễ làm nhưng phần lớn yêu cầu nhận lại dữ liệu cũ. **Push**: máy chủ gửi khi sự kiện xảy ra. **Streaming**: một chuỗi thông điệp liên tục. Phân biệt bằng phía khởi tạo và số thông điệp trong một tương tác.
- **WebSocket** là giao thức kết nối hai chiều lâu dài. **SignalR** là thư viện .NET cung cấp Hub, nhóm, kết nối lại và lựa chọn WebSocket / Server-Sent Events / Long Polling.
- REST tạo lượt nộp và đọc trạng thái hiện tại; SignalR báo thay đổi tới kết nối đang theo dõi. REST là nguồn sự thật nếu thông điệp bị lỡ.
- Mẫu thiết kế tính năng realtime: sự kiện nào xảy ra; ai nhận; gửi dữ liệu tối thiểu nào; khôi phục ra sao khi rớt mạng.

## Cấu trúc

```text
PingSignalR/                         # Echo, Broadcast, Group, vòng đời kết nối
  PingSignalR.slnx
RealtimeSubmissionSystem/            # REST + SignalR cho EduSubmit
  RealtimeSubmissionSystem.slnx
  requests.http                       # 9 ca kiểm tra REST/negotiate
```

## Build và chạy

```powershell
dotnet build PingSignalR/PingSignalR.slnx
dotnet build RealtimeSubmissionSystem/RealtimeSubmissionSystem.slnx
```

Mỗi solution có hai project (API và console client). Các lệnh sau chạy từ thư mục repo.

### PingSignalR (cổng 5200)

Terminal 1:

```powershell
dotnet run --project PingSignalR/src/Ping.Api --no-launch-profile --urls http://localhost:5200
```

Terminal 2 và 3, khởi động An trước Bình để kiểm tra tin nhóm và broadcast:

```powershell
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 An
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 Binh
```

Kết quả đã quan sát: An nhận `[Group]` và `[Broadcast]` của Bình, không nhận `[Echo]` của Bình. Khi máy chủ chạy lại, client báo `Dang ket noi lai...` rồi `Da ket noi lai`; mã kết nối mới nên client tự vào lại nhóm.

### EduSubmit realtime (cổng 5000)

Terminal 1:

```powershell
dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeApi --no-launch-profile --urls http://localhost:5000
```

Terminal 2:

```powershell
dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeClient -- http://localhost:5000 SV001
```

Client lấy bài tập, POST lượt nộp, theo dõi nhóm `submission:{id}`, nhận `processing` và `graded`, rồi GET REST để chốt trạng thái. Kết quả chạy thật: `SignalR = graded, REST = graded`.

## 9 request trong `requests.http`

| # | Yêu cầu | Kết quả |
|---|---|---|
| 1 | GET `/` | 200 |
| 2 | GET `/api/assignments` | 200 |
| 3 | POST lượt nộp hợp lệ | 201 + Location + `received` |
| 4 | GET lượt nộp theo id | 200; trạng thái đổi `received → processing → graded` |
| 5 | POST thiếu dữ liệu | 400 |
| 6 | POST assignment không tồn tại | 404 |
| 7 | GET id không tồn tại | 404 |
| 8 | POST `/hubs/submissions/negotiate` | 200 + connectionId |
| 9 | POST sai đường dẫn `/hub/submissions/negotiate` | 404 |

Đã gọi thật: các mã trả về lần lượt `200, 200, 201, 400, 404, 404, 200, 404` cho các ca tương ứng.

## Thí nghiệm phân tán

| Thí nghiệm | Cách chạy / kết quả thật | Giải thích |
|---|---|---|
| Hai lượt nộp | `dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeClient -- http://localhost:5000 SV001 --two`; lượt SV001 thành `graded`, SV002 thành `failed` | Mỗi id vào nhóm riêng; URL không phải HTTPS được cố ý chấm `failed` để làm bài tập mở rộng. |
| Máy chủ chưa chạy | Dừng API rồi chạy client | `HttpRequestException: actively refused`; `WithAutomaticReconnect` không tự thử lại lần kết nối đầu. |
| Sai đường dẫn hub | Thêm `--wrong-hub` vào lệnh client | Negotiate trả 404 do `/hub/submissions` không khớp `/hubs/submissions`. |
| Sai tên sự kiện | Thêm `--wrong-event` | Không nhận push, không có lỗi; sau 10 giây REST vẫn đọc được `graded`. |
| Watch muộn | Thêm `--late-watch` | Client đợi 4 giây trước khi vào nhóm nên lỡ sự kiện; hết thời gian chờ nhưng REST vẫn trả `graded`. |

Các cờ `--two`, `--wrong-hub`, `--wrong-event`, `--late-watch` giúp chạy lại thí nghiệm. Cờ lỗi chỉ thay đổi hành vi máy khách trong lần chạy đó, không sửa hợp đồng máy chủ.

## Ba câu hỏi đầu giờ

1. **Máy chủ có chủ động báo được không?** Có. Máy khách mở kết nối SignalR; máy chủ dùng `Clients.Group(...).SendAsync(...)` hoặc `IHubContext` để đẩy thông báo.
2. **REST và SignalR chia việc thế nào?** REST tạo/đọc tài nguyên, trả mã HTTP rõ ràng. SignalR gửi sự kiện trạng thái tới đúng người theo dõi. SignalR không thay REST.
3. **Rớt mạng đúng lúc báo graded thì sao?** Thông báo có thể bị lỡ. Sau reconnect, theo dõi lại và gọi `GET /api/submissions/{id}` để đồng bộ trạng thái hiện tại.

## Nhật ký build

```text
PingSignalR: Build succeeded. 0 Warning(s), 0 Error(s).
RealtimeSubmissionSystem: Build succeeded. 0 Warning(s), 0 Error(s).
```

`bin/` và `obj/` được loại khỏi Git bởi `.gitignore`. Ảnh chụp màn hình theo mẫu giảng viên chưa được tạo trong môi trường này; README ghi lại lệnh và kết quả chạy thật để có thể đối chiếu. Trước khi nộp qua lớp, chụp các màn hình yêu cầu ở slide 23 vào thư mục `anh/` và chèn vào mục tương ứng.

## Git và nộp bài

Repo Git đã được khởi tạo tại thư mục này. Các thay đổi được commit riêng theo từng phần/bước. Chưa cấu hình `git remote` vì slide không cung cấp URL repo lớp và tên/MSSV để tạo Pull Request; sau khi có remote của lớp, tạo PR với tiêu đề `Bai3 - MSSV - Ho ten`.
