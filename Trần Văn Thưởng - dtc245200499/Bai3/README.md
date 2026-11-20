# Bài 3 – PingSignalR

**Sinh viên:** Trần Văn Thưởng — **MSSV:** dtc245200499  
**Chủ đề trước đó:** Hệ thống thư viện và đặt mượn sách (Đề 20). Bài này tập trung vào giao tiếp thời gian thực bằng SignalR theo mẫu `PingSignalR` của bài giảng.

## Mục tiêu

Tách máy chủ SignalR và máy khách thành hai tiến trình .NET 10. Máy khách gọi `Echo`, gửi tin cho mọi kết nối hoặc một nhóm; máy chủ phát thông báo vòng đời kết nối. REST phù hợp để truy vấn tài nguyên, còn SignalR dùng để đẩy sự kiện khi thay đổi xảy ra.

## Cấu trúc

```text
Bai3/
├── PingSignalR.slnx
├── README.md
└── src/
    ├── Ping.Api/       # Máy chủ tại http://localhost:5200
    └── Ping.Client/    # Máy khách console
```

## Khởi tạo (Bước 1.1–1.2)

Tạo solution và hai project `Ping.Api`, `Ping.Client`; thêm cả hai project vào solution; thêm `Microsoft.AspNetCore.SignalR.Client` phiên bản `10.0.15` cho máy khách. Máy chủ dùng shared framework ASP.NET Core nên không cần cài package SignalR riêng. `.gitignore` loại `bin/` và `obj/` khỏi Git.

Build ban đầu: `dotnet build` — **Build succeeded, 0 Warning(s), 0 Error(s)**.

## Chạy

Mở hai terminal tại thư mục `Bai3`.

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
