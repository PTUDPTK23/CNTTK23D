# Nhật ký Bài 3 – SignalR (PingSignalR)

## Các bước đã làm

| Bước | Nội dung | Kiểm tra | Ảnh |
|---|---|---|---|
| 1.1 | Tạo `PingSignalR.slnx`, `src/Ping.Api` (web), `src/Ping.Client` (console) | `ls src` ra đúng Ping.Api, Ping.Client | `b1-1-tao-solution.png` |
| 1.2 | `dotnet sln add` 2 project; thêm `Microsoft.AspNetCore.SignalR.Client` 10.0.15 cho máy khách; `dotnet new gitignore`; build | Build succeeded, 0 Error; csproj có PackageReference | `b1-2-goi-va-build.png` |
| 2 | Máy chủ: `AddSignalR()` + `MapHub<PingHub>("/hubs/ping")` | `GET /` → `{"service":"PingApi","status":"running"}`; negotiate trả `connectionId` + WebSockets, ServerSentEvents, LongPolling; `GET /hubs/ping` → 400 `Connection ID required` | `b2-terminal1-may-chu.png`, `b2-terminal2-negotiate.png` |
| 3 | Hub có `Echo` trả lời riêng người gọi (`Clients.Caller`) | Build 0 Error, máy chủ chạy lại cổng 5200 | `b3-echo-build-chay-may-chu.png` |
| 4 | Máy khách: `HubConnectionBuilder` → `On("EchoResponse")` → `StartAsync` → `InvokeAsync("Echo")` | In `[Echo] Echo: Xin chao SignalR tu An` | `b4-client-echo-an.png` |
| 5 | Hub thêm `Broadcast` (`Clients.All`), `JoinGroup` (`Groups.AddToGroupAsync`), `SendToGroup` (`Clients.Group`); máy khách thêm `On("BroadcastMessage")`, `On("GroupMessage")` trước `StartAsync` và 3 lời gọi sau `Echo` | Build 0 Error; chạy máy chủ, An, rồi Bình | `b5-build.png`, `b5-terminal1-may-chu.png`, `b5-terminal2-an.png`, `b5-terminal3-binh.png` |

## Thí nghiệm: sai tên ở hai chiều (slide 31)

| # | Thay đổi | Dự đoán | Hiện tượng thực tế | Minh chứng |
|---|---|---|---|---|
| 1 | **Chiều lên**: máy khách gọi `InvokeAsync("Ech0", ...)` trong khi hub có `Echo` | Lỗi khi chạy | Kết nối vẫn thành công (`An da ket noi den PingApi.`), nhưng ngay lời gọi thì ném `HubException: Failed to invoke 'Ech0' due to an error on the server. HubException: Method does not exist.` tại `Program.cs` dòng 22; chương trình dừng | `b31-sai-ten-chieu-len.png`, `tn-ten-chieu-len.txt` |
| 2 | **Chiều xuống**: hub gửi `SendAsync("EchoRespone", ...)` trong khi máy khách nghe `On("EchoResponse")` | Không in gì, không báo lỗi | Build không lỗi, chạy không lỗi, exit code 0; chỉ in `An da ket noi…` và `Dang cho thong diep…`, **không có dòng `[Echo]`** | `b31-may-chu-sai-ten-xuong.png`, `b31-sai-ten-chieu-xuong.png`, `tn-ten-chieu-xuong.txt` |
| 3 | Sửa lại đúng tên ở cả hai chiều, build và chạy lại máy chủ | In lại `[Echo]` | `[Echo] Echo: Xin chao SignalR tu An` | `b31-may-chu-sua-lai.png`, `b31-sua-lai-dung-ten.png`, `tn-ten-sua-lai.txt` |

**Nhận xét.** Hai chiều sai tên cho hai kiểu lỗi khác nhau. Chiều lên do máy chủ kiểm tra: hub không có phương thức `Ech0` nên trả lỗi về, máy khách nhận `HubException` và dừng — dễ phát hiện. Chiều xuống chỉ là gửi một thông điệp mang tên mà máy khách không đăng ký nghe; SignalR bỏ qua thông điệp đó mà không báo cho ai, nên chương trình vẫn chạy "bình thường" nhưng thiếu dữ liệu — rất khó phát hiện. Vì vậy cần ghi bảng hợp đồng tên (`Echo`, `EchoResponse`) trước khi code, và khi không nhận được gì thì so tên ở hai phía trước tiên.

## Bước 5: ba phạm vi gửi – An và Bình (slide 32–35)

Chạy theo thứ tự: Terminal 1 máy chủ → Terminal 2 An → Terminal 3 Bình.

| Thông điệp | An thấy? | Bình thấy? | Vì sao |
|---|---|---|---|
| `[Echo] … tu An` | ✓ | ✗ | `Clients.Caller`: chỉ người gọi |
| `[Group] … tu An` (An vào nhóm, thông báo nhóm) | ✓ | ✗ | Bình chưa kết nối lúc An gửi |
| `[Broadcast] Xin chao tat ca tu An` | ✓ | ✗ | Bình chưa kết nối lúc An gửi |
| `[Echo] … tu Binh` | ✗ | ✓ | `Clients.Caller`: chỉ người gọi |
| `[Group] ldQkspsz da tham gia nhom`, `[Group] Thong bao nhom tu Binh` | ✓ | ✓ | `Clients.Group("lop-hoc-01")`: cả hai đã ở trong nhóm |
| `[Broadcast] Xin chao tat ca tu Binh` | ✓ | ✓ | `Clients.All`: mọi kết nối đang mở, kể cả người gửi |

**Kiểm tra slide:** cửa sổ An có `[Broadcast] Xin chao tat ca tu Binh` nhưng **không** có `[Echo]` của Bình ✓ (`b5-terminal2-an.png`).

**Nhận xét.** Cùng một hub nhưng ba phạm vi gửi cho ba tập người nhận khác nhau: `Caller` chỉ một kết nối, `Group` các kết nối đã vào nhóm, `All` mọi kết nối. SignalR không lưu tin cho người đến sau: Bình không thấy tin An gửi trước khi Bình kết nối. Dòng `Dang cho thong diep…` có thể in trước hoặc sau các dòng nhận được vì thông điệp đến trên luồng riêng của kết nối, song song với luồng chính.

## Thử thách 3 phút: ba máy khách, một nhóm (slide 36)

Tình trạng: An và Bình ở nhóm `lop-hoc-01`; Chi không ở nhóm nào.

| Lời gọi | An | Bình | Chi | Phạm vi trong hub |
|---|---|---|---|---|
| ① An gọi `Echo("hi")` | ✓ | ✗ | ✗ | `Clients.Caller` – chỉ người gọi |
| ② Chi gọi `SendToGroup("lop-hoc-01", "x")` | ✓ | ✓ | ✗ | `Clients.Group("lop-hoc-01")` – chỉ thành viên nhóm |
| ③ Bình gọi `Broadcast("y")` | ✓ | ✓ | ✓ | `Clients.All` – mọi kết nối, kể cả người gửi |

**Lý do câu ②.** `SendToGroup` gửi tới các kết nối **đang ở trong nhóm**, không phụ thuộc người gọi có ở trong nhóm hay không. Chi gọi được và tin đến An, Bình, nhưng Chi không nhận vì Chi không phải thành viên. Hub ví dụ không kiểm tra quyền, nên ai cũng gửi được vào nhóm — trong ứng dụng thật cần kiểm tra người gửi trước khi cho gửi.

Đối chiếu đáp án slide 37: khớp cả 9 ô.

## Bước 6: vòng đời kết nối (slide 38)

- Hub ghi đè `OnConnectedAsync` / `OnDisconnectedAsync`: in `[Hub] + … ket noi` / `[Hub] - … ngat` ở máy chủ và gửi `SystemMessage` tới mọi kết nối (`Clients.All`); nhớ gọi `base`.
- Máy khách thêm `On("SystemMessage")` và 3 sự kiện vòng đời `Reconnecting`, `Reconnected`, `Closed`, đăng ký trước `StartAsync`.

| Kiểm tra | Kết quả | Ảnh |
|---|---|---|
| Build | Build succeeded, cả 2 project | `b6-build.png` |
| Terminal 1 in `[Hub] + … ket noi` mỗi khi một máy khách mở | `[Hub] + 5Y-Svh_M ket noi` (An), `[Hub] + FFRfEfjW ket noi` (Bình) | `b6-terminal1-may-chu.png` |
| Máy khách nhận `[System]` | An thấy `[System] Client 5Y-Svh_M da ket noi.` (chính mình) và `[System] Client FFRfEfjW da ket noi.` (Bình vào sau) | `b6-terminal2-an.png`, `b6-terminal3-binh.png` |

## Thí nghiệm Bước 6: tắt máy chủ, máy khách tự kết nối lại (slide 39)

| Thời điểm | Thao tác | Hiện tượng | Ảnh |
|---|---|---|---|
| 09:10:07 | Tắt máy chủ (dừng tiến trình `Ping.Api`) khi An và Bình đang kết nối | Terminal 1 dừng, về dấu nhắc | `b39-1-may-chu-da-tat.png` |
| +4 s | – | An in `[Lifecycle] Dang ket noi lai...` | `b39-2-an-dang-ket-noi-lai.png` |
| 09:10:18 | Chạy lại máy chủ ở cổng 5200 | Máy chủ in `[Hub] + GumIlr3X ket noi`, `[Hub] + E6hHNoch ket noi` | `b39-3-may-chu-chay-lai.png` |
| ~09:11 | – | An in `[Lifecycle] Da ket noi lai: GumIlr3X`, rồi `[System] Client E6hHNoch da ket noi.` (Bình) và `[System] Client GumIlr3X da ket noi.` | `b39-4-an-da-ket-noi-lai.png`, `b39-5-binh-da-ket-noi-lai.png` |

**Kiểm tra slide:** có đủ 2 dòng `Dang ket noi lai...` và `Da ket noi lai`, ConnectionId của An đổi từ `5Y-Svh_M` sang `GumIlr3X` ✓.

**Nhận xét.**
- `WithAutomaticReconnect()` thử lại theo lịch mặc định 0 s, 2 s, 10 s, 30 s; máy chủ chạy lại sau khoảng 11 s nên một trong các lần thử sau đã thành công. Hết 4 lần mà không nối được thì chuyển sang `Closed`.
- ConnectionId mới nghĩa là máy chủ coi đây là **kết nối mới**: `OnConnectedAsync` chạy lại (máy chủ in `[Hub] +` và gửi `[System]`).
- Nhóm gắn với ConnectionId cũ nên **bị mất**; muốn ở lại nhóm phải gọi lại `JoinGroup` trong sự kiện `Reconnected`.
- Tin gửi trong lúc rớt kết nối **mất hẳn**, SignalR không lưu để gửi lại.

## Bước 7: ba thí nghiệm lỗi (slide 40)

| # | Làm gì | Thấy gì | Giải thích | Ảnh |
|---|---|---|---|---|
| 1 | Chạy máy khách khi máy chủ **chưa chạy** (`curl` tới cổng 5200 không có phản hồi) | `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5200)`; chương trình dừng | `StartAsync` gọi negotiate qua HTTP, không có tiến trình nghe cổng 5200 nên hệ điều hành từ chối kết nối. `WithAutomaticReconnect` chỉ cứu kết nối **đã mở**, không giúp lần mở đầu tiên | `b7-1-may-chu-chua-chay.png` |
| 2 | Sửa `WithUrl` thành `/hub/ping` (thiếu `s`) | `HttpRequestException: Response status code does not indicate success: 404 (Not Found)` tại `HttpConnection.NegotiateAsync` | Máy chủ đang chạy nhưng không có endpoint `/hub/ping/negotiate`; lỗi xảy ra ở bước negotiate. Đường dẫn ở `WithUrl` phải khớp `MapHub<PingHub>("/hubs/ping")` | `b7-may-chu.png`, `b7-2-sai-duong-dan-hub.png` |
| 3 | Sửa máy khách `On("EchoResponse")` thành `On("EchoRespone")` (máy chủ vẫn gửi `"EchoResponse"`) | Không có lỗi nào; mọi dòng khác (`[System]`, `[Group]`, `[Broadcast]`) vẫn in, chỉ **thiếu dòng `[Echo]`** | Thông điệp `EchoResponse` vẫn đến nhưng không có hàm nào đăng ký tên đó nên bị bỏ qua im lặng. Khi "không nhận được gì", việc đầu tiên là so tên hai phía | `b7-3-sai-ten-thong-diep.png` |
| – | Sửa lại đúng cả hai chỗ, build và chạy lại | In đủ `[Echo]`, `[Group]`, `[Broadcast]` | Code đã trả về như trước thí nghiệm | `b7-4-sua-lai-binh-thuong.png` |

Hai lỗi đầu là **lỗi kết nối** (ném ngoại lệ, chương trình dừng – dễ thấy); lỗi thứ ba là **lỗi hợp đồng tên** (không ngoại lệ – khó thấy).

## Phần 3 – Áp dụng cho EduSubmit (slide 42–43)

Mục tiêu (slide 42): giữ nguyên REST của Bài 2 để tạo và đọc lượt nộp; thêm hub `/hubs/submissions` để máy chủ đẩy trạng thái `processing`, `graded` tới đúng nhóm `submission:{id}`.

| Khái niệm | PingSignalR (Phần 2) | EduSubmit realtime (Phần 3) |
|---|---|---|
| Hub | `PingHub`, `/hubs/ping` | `SubmissionHub`, `/hubs/submissions` |
| Vào nhóm | `JoinGroup("lop-hoc-01")` | `WatchSubmission(id)` → nhóm `submission:{id}` |
| Tên thông điệp | `EchoResponse`, `GroupMessage`… | `SubmissionStatusChanged` |
| Dữ liệu gửi kèm | `string` | `record SubmissionStatusMessage(Id, Status, UpdatedAt)` |
| **Ai kích hoạt gửi** | Máy khách gọi phương thức hub | **MỚI:** tác vụ chấm nền, qua `IHubContext` |
| REST đi kèm | chỉ `GET /` | Toàn bộ API Bài 2: `GET`, `POST` 201, `GET {id}` |

**Dòng mới duy nhất: "Ai kích hoạt gửi".** Ở PingSignalR, mọi thông điệp đều sinh ra *bên trong* một phương thức hub mà máy khách vừa gọi (`Echo`, `Broadcast`…), nên hub có sẵn `Clients`. Ở EduSubmit, trạng thái đổi do **tác vụ chấm chạy nền** sau khi `POST` đã trả 201 — lúc đó không có máy khách nào đang gọi hub, không có đối tượng hub nào để dùng `Clients`. Vì vậy cần `IHubContext<SubmissionHub>` (lấy qua dependency injection) để gửi thông điệp **từ bên ngoài hub**, ví dụ `hubContext.Clients.Group($"submission:{id}").SendAsync("SubmissionStatusChanged", …)`. Mọi kỹ thuật còn lại (MapHub, nhóm, On/StartAsync, phạm vi gửi) đã làm ở Phần 2.

## Thử thách: 4 câu hỏi cho EduSubmit trước khi code (slide 44–45)

| Câu | Trả lời | Câu trả lời hay gặp nhưng chưa đủ (slide 45) |
|---|---|---|
| ① Sự kiện nào kích hoạt thông điệp? | **Mỗi lần trạng thái lượt nộp đổi**: `received` → `processing` → `graded` | "Khi sinh viên nộp bài" – nộp bài chỉ tạo lượt nộp, còn thông báo gắn với lúc trạng thái đổi |
| ② Gửi cho ai? | **`Clients.Group("submission:{id}")`** – chỉ các kết nối đã gọi `WatchSubmission(id)` cho đúng lượt nộp đó | `Clients.All` – lộ trạng thái bài của người khác và tốn đường truyền |
| ③ Gửi kèm dữ liệu gì (tối thiểu)? | **Mã lượt nộp, trạng thái mới, thời điểm đổi** → `SubmissionStatusMessage(Id, Status, UpdatedAt)` | Chỉ chữ `"graded"` – máy khách không biết là lượt nộp nào, đổi lúc nào |
| ④ Mất kết nối thì lấy lại trạng thái thế nào? | **Gọi lại `GET /api/submissions/{id}`** (và gọi lại `WatchSubmission(id)` sau khi kết nối lại) | "SignalR tự gửi lại tin đã lỡ" – SignalR không lưu tin; kết nối lại có ConnectionId mới (đã thấy ở thí nghiệm slide 39) |

**Nguyên tắc rút ra:** REST là **nguồn sự thật** (trạng thái lưu ở máy chủ, đọc bằng `GET`); SignalR chỉ **báo có thay đổi** để máy khách không phải hỏi liên tục, không thay thế REST.

## Bản thiết kế kênh thời gian thực và kế hoạch (slide 46–47)

Mỗi câu trả lời ở slide 44–45 thành một đoạn code:

| Câu | Thiết kế | Code dự kiến |
|---|---|---|
| ① Khi nào gửi | Trạng thái lượt nộp đổi: `received` → `processing` → `graded` | `UpdateStatusAsync(id, status, …)` |
| ② Gửi cho ai | Chỉ máy khách đang theo dõi đúng lượt nộp | `Clients.Group("submission:{id}")` |
| ③ Gửi gì | Mã lượt nộp, trạng thái mới, thời điểm đổi | `record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt)` |
| ④ Lấy lại trạng thái | Gọi lại REST | `http.GetFromJsonAsync<SubmissionDto>(…)` trên `GET /api/submissions/{id}` |

Kế hoạch ứng dụng `RealtimeSubmissionSystem` (cổng 5000 như Bài 2, hub `http://localhost:5000/hubs/submissions`):

| Bước | Việc | Kết quả thấy được | Câu hỏi khung |
|---|---|---|---|
| 1 | Tạo solution `RealtimeSubmissionSystem` | Build succeeded | – |
| 2 | REST của Bài 2 + `UpdatedAt` + `lock` | `curl POST` → 201 `received` | ④ nguồn sự thật |
| 3 | `SubmissionHub` + nhóm `submission:{id}` | negotiate trả `connectionId` | ② |
| 4 | `IHubContext` + mô phỏng chấm nền | `GET` sau 3 giây → `graded` | ① ③ |
| 5 | Máy khách: hai kênh, đăng ký `On` | `Da ket noi SignalR.` | ③ |
| 6 | Máy khách: POST → Watch → chờ → GET | `processing`, `graded` hiện ra | ② ④ |
| 7 | Thí nghiệm phân tán | Giải thích 5 hiện tượng | ② ④ |
| 8 | `requests.http`, bài tập, nộp PR | PR `Bai3 - MSSV - Họ tên` | – |
