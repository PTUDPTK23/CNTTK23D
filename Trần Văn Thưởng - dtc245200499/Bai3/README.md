# Bài 3 – SignalR và EduSubmit thời gian thực

**Sinh viên:** Trần Văn Thưởng — **MSSV:** dtc245200499  
**Chủ đề trước đó:** Hệ thống thư viện và đặt mượn sách (Đề 20). Bài này tập trung vào giao tiếp thời gian thực bằng SignalR theo mẫu `PingSignalR` của bài giảng.

## Mục tiêu

Thư mục Bài 3 có hai solution .NET 10: `PingSignalR` (luyện hub, Echo, broadcast, group và reconnect) và `RealtimeSubmissionSystem` (giữ REST của Bài 2, dùng SignalR để báo trạng thái chấm đến đúng sinh viên). REST tạo và đọc lượt nộp; SignalR đẩy thay đổi phát sinh.

## Cấu trúc

```text
Bai3/
├── README.md
├── PingSignalR/
│   ├── PingSignalR.slnx
│   └── src/Ping.Api, Ping.Client/
└── RealtimeSubmissionSystem/
    ├── RealtimeSubmissionSystem.slnx
    ├── requests.http
    └── src/Submission.RealtimeApi, Submission.RealtimeClient/
```

## Khởi tạo (Bước 1.1–1.2)

Tạo solution và hai project `Ping.Api`, `Ping.Client`; thêm cả hai project vào solution; thêm `Microsoft.AspNetCore.SignalR.Client` phiên bản `10.0.15` cho máy khách. Máy chủ dùng shared framework ASP.NET Core nên không cần cài package SignalR riêng. `.gitignore` loại `bin/` và `obj/` khỏi Git.

Build ban đầu: `dotnet build` — **Build succeeded, 0 Warning(s), 0 Error(s)**.

## Chạy

Mở hai terminal tại thư mục `Bai3/PingSignalR`.

```powershell
dotnet run --project src/Ping.Api --urls http://localhost:5200
```

```powershell
dotnet run --project src/Ping.Client -- http://localhost:5200 An
```

Mở terminal thứ ba để chạy máy khách thứ hai, ví dụ `Binh`. Nhấn Enter trong máy khách để thoát.

## Tiến độ theo bước

- Bước 2: đăng ký SignalR và ánh xạ hub `/hubs/ping`; kiểm tra `/` và `/negotiate`.
- Bước 3: `Echo` trả lời riêng người gọi bằng `EchoResponse`.
- Bước 4: máy khách đăng ký sự kiện, kết nối và gọi `Echo`.
- Bước 5: thêm `Broadcast`, `JoinGroup`, `SendToGroup` và phạm vi `Caller` / `All` / `Group`.
- Bước 6: ghi nhận kết nối/ngắt kết nối, tự kết nối lại và vào lại nhóm.
- Bước 7: thử ba lỗi có chủ đích và ghi kết quả/bài học ở cuối README.

## Hợp đồng hub và phạm vi gửi

| Phương thức hub | Phạm vi / sự kiện | Ý nghĩa |
|---|---|---|
| `Echo(message)` | `Clients.Caller` → `EchoResponse` | Chỉ kết nối vừa gọi nhận phản hồi. |
| `Broadcast(message)` | `Clients.All` → `BroadcastMessage` | Mọi kết nối đang mở nhận, gồm cả người gửi. |
| `JoinGroup(groupName)` | `Groups.AddToGroupAsync`; `Clients.Group` → `GroupMessage` | Đăng ký connection vào nhóm và báo thành viên. |
| `SendToGroup(groupName, message)` | `Clients.Group` → `GroupMessage` | Chỉ connection thuộc nhóm nhận tin. |

Tên sự kiện ở `SendAsync` và `connection.On` là hợp đồng chuỗi; sai tên có thể không báo lỗi nhưng client sẽ im lặng.

## Kết quả chạy thực tế

- `GET /` trả HTTP 200 với `{"service":"PingApi","status":"running"}`.
- `POST /hubs/ping/negotiate?negotiateVersion=1` trả `connectionId` và đủ ba transport: WebSockets, Server-Sent Events, Long Polling.
- Chạy hai client `An` và `Binh`: `Binh` nhận `[Group] Thong bao nhom tu An` và `[Broadcast] Xin chao tat ca tu An`; `Binh` không nhận `[Echo]` của An. Đây là kiểm chứng khác nhau giữa Caller, Group và All.
- Tắt rồi bật lại máy chủ trong khi client đang mở: client in `[Lifecycle] Dang ket noi lai...` rồi `[Lifecycle] Da ket noi lai: ...` với ConnectionId mới. Client gọi `JoinGroup` lại trong callback `Reconnected`, vì membership của nhóm gắn với connection cũ.

## Bước 7 – ba thí nghiệm lỗi

| Thí nghiệm | Làm gì | Kết quả chạy thật | Bài học về kết nối phân tán |
|---|---|---|---|
| Máy chủ chưa chạy | Dừng `Ping.Api`, sau đó khởi động client. | `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5200)` ngay tại `StartAsync`. | `WithAutomaticReconnect()` chỉ xử lý kết nối đã mở rồi bị đứt; lỗi kết nối lần đầu cần retry riêng ở tầng khởi động. |
| Sai tuyến hub | Tạm đổi client sang `/hub/ping` thay vì `/hubs/ping`. | `HttpRequestException: Response status code does not indicate success: 404 (Not Found)` trong bước negotiate. | URL của client phải khớp chính xác với tuyến `MapHub`; lỗi xuất hiện trước khi WebSocket được mở. |
| Sai tên sự kiện | Tạm đổi `On("EchoResponse")` thành `On("EchoRespone")`. | Không có exception; không xuất hiện dòng `[Echo]`, các sự kiện System/Group/Broadcast khác vẫn chạy. | Tên sự kiện là hợp đồng không được compiler kiểm tra. Khi chỉ một thông điệp mất, kiểm tra tên `SendAsync` và `On` trước. |

Sau mỗi thí nghiệm, đã phục hồi source chuẩn và build lại thành công. Build cuối: **0 Warning(s), 0 Error(s)**. Các thư mục `bin/` và `obj/` được loại khỏi Git bằng `.gitignore`.

## Phần 3 – RealtimeSubmissionSystem (Bước 1–8)

### Cấu trúc và chạy

Solution thứ hai mô phỏng quy trình nộp bài. API có REST trên cổng `5000` và hub tại `/hubs/submissions`; máy khách console dùng `HttpClient` và SignalR Client. Máy chủ lưu dữ liệu trong bộ nhớ, có một bài đang mở (ID 1) và một bài đã đóng (ID 2).

```powershell
cd RealtimeSubmissionSystem
dotnet build
dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000
```

Mở terminal thứ hai:

```powershell
cd RealtimeSubmissionSystem
dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 DTC245200499
```

Máy khách mở SignalR trước, lấy bài tập và tạo lượt nộp bằng REST, gọi `WatchSubmission(id)`, chờ tối đa 10 giây để nhận `processing` và `graded`, rồi đọc trạng thái cuối bằng REST. Tác vụ chấm giả lập đổi trạng thái sau 1 giây và 2 giây tiếp theo. `IHubContext<SubmissionHub>` gửi thông báo từ tác vụ nền; chỉ nhóm `submission:{id}` nhận thông báo của lượt đó.

### Hợp đồng API

| Phương thức | URI | Kết quả |
|---|---|---|
| GET | `/api/assignments` | 200, danh sách bài tập |
| POST | `/api/assignments/{assignmentId}/submissions` | 201 + `Location` khi hợp lệ; 400 khi bài đã đóng hoặc dữ liệu thiếu; 404 nếu không có bài |
| GET | `/api/submissions/{id}` | 200 trạng thái hiện tại; 404 nếu không tìm thấy |
| POST | `/hubs/submissions/negotiate` | Bắt tay SignalR |

`requests.http` có 11 ca gồm đủ 200, 201, 400, 404 và negotiate. Ca cuối dùng cổng `5200`, cần chạy `PingSignalR` trong terminal riêng.

### Bước 7 – năm thí nghiệm phân tán

| Thí nghiệm | Kết quả quan sát | Bài học |
|---|---|---|
| Hai client chạy đồng thời | Mỗi client chỉ nhận thông báo cho ID lượt nộp mình theo dõi. | Nhóm theo ID tách người nhận và giảm dữ liệu gửi thừa. |
| Máy chủ chưa chạy khi mở client | `StartAsync` báo `HttpRequestException` / connection refused. | Tự kết nối lại chỉ cứu một kết nối từng mở; lần nối đầu cần retry riêng. |
| Sửa URL thành `/hub/submissions` | Negotiate trả 404. | URL client phải khớp chính xác `MapHub`. |
| Đổi tên `On` thành `SubmissionStatusChange` | Client không báo lỗi nhưng không thấy trạng thái. | Tên sự kiện `SendAsync` và `On` là hợp đồng chuỗi hai phía. |
| Chờ khoảng 4 giây rồi mới gọi `WatchSubmission` | Không nhận được push đã gửi trước lúc vào nhóm; REST đọc lại vẫn trả `graded`. | SignalR không lưu tin; REST là nguồn sự thật và cần đọc lại khi lỡ thông báo. |

Khi chạy thí nghiệm, chỉ sửa tạm client hoặc dùng client thứ hai rồi khôi phục lại source chuẩn trước khi build/commit.

### Ba câu hỏi đầu giờ

1. Máy chủ có thể chủ động báo cho client bằng SignalR sau khi client mở kết nối lâu dài; REST thường trả lời khi client gửi yêu cầu.
2. REST dùng để tạo lượt nộp và đọc trạng thái hiện tại. SignalR bổ sung thông báo tức thời khi trạng thái đổi, không thay thế REST.
3. Nếu đứt mạng lúc trạng thái chuyển sang `graded`, thông báo có thể bị lỡ. Client kết nối lại, đăng ký theo dõi lại và gọi `GET /api/submissions/{id}` để lấy trạng thái mới nhất.

### Kiểm tra trước khi nộp

```powershell
dotnet build PingSignalR/PingSignalR.slnx
dotnet build RealtimeSubmissionSystem/RealtimeSubmissionSystem.slnx
```

Đã có 2 solution, 4 project, package `Microsoft.AspNetCore.SignalR.Client` phiên bản `10.0.15`, và loại `bin/`, `obj/` bằng `.gitignore`. Nhật ký terminal dưới đây là kết quả kiểm tra của máy hiện tại; chưa đính kèm ảnh chụp màn hình lớp học.
