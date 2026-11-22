# Bài 3 - SignalR

**Sinh viên:** Dương Thu Huyền  
**Mã sinh viên:** dtc245200714

Bài gồm hai solution .NET 10:

- `PingSignalR`: Hub `Echo`, Broadcast, Group, vòng đời kết nối và tự đăng ký lại nhóm sau khi reconnect.
- `RealtimeSubmissionSystem`: REST tạo/đọc lượt nộp, Hub theo dõi group theo từng ID, tác vụ giả lập chấm và đẩy trạng thái `processing` rồi `graded` hoặc `failed`.

## Build

Chạy từ thư mục `Bai3`:

```powershell
dotnet build PingSignalR/src/Ping.Api/Ping.Api.csproj
dotnet build PingSignalR/src/Ping.Client/Ping.Client.csproj
dotnet build RealtimeSubmissionSystem/src/Submission.RealtimeApi/Submission.RealtimeApi.csproj
dotnet build RealtimeSubmissionSystem/src/Submission.RealtimeClient/Submission.RealtimeClient.csproj
```

Client cần NuGet `Microsoft.AspNetCore.SignalR.Client` phiên bản `10.0.15`.

## PingSignalR

Mở terminal 1:

```powershell
dotnet run --project PingSignalR/src/Ping.Api --urls http://localhost:5200
```

Mở terminal 2 và 3:

```powershell
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 An
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 Binh
```

Mỗi client nhận Echo riêng. Hai client cùng nhận Group và Broadcast sau khi cả hai đã kết nối. Nhấn Enter ở client để đóng kết nối.

Kiểm tra negotiate:

```powershell
curl.exe -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"
```

## EduSubmit realtime

Terminal 1:

```powershell
dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeApi --urls http://localhost:5000
```

Terminal 2:

```powershell
dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeClient -- http://localhost:5000 SV001
```

Kết quả dự kiến: REST tạo lượt nộp với `received`, SignalR báo `processing` rồi `graded`, và REST đọc lại trạng thái cuối. API chỉ lưu trong bộ nhớ; khởi động lại API sẽ xóa các lượt nộp.

Tuỳ chọn thực hành:

```powershell
# Hai lượt nộp đồng thời, kiểm tra thông báo không bị trộn
dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeClient -- http://localhost:5000 SV001 --two

# URL repository không phải https://, kết quả chấm cuối là failed
dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeClient -- http://localhost:5000 SV001 --fail
```

Các request HTTP và mã trạng thái dự kiến có trong [`RealtimeSubmissionSystem/requests.http`](RealtimeSubmissionSystem/requests.http).

## Thiết kế realtime: 4 câu hỏi

1. **Sự kiện nào?** Mỗi lần trạng thái đổi: `received` → `processing` → `graded` hoặc `failed`.
2. **Gửi cho ai?** Chỉ các kết nối đã gọi `WatchSubmission(id)` và ở trong group `submission:{id}`.
3. **Gửi gì?** `SubmissionStatusMessage` chứa ID, trạng thái và thời điểm cập nhật.
4. **Mất kết nối thì sao?** SignalR tự kết nối lại nhưng không phát lại tin cũ. Client đăng ký theo dõi lại; REST `GET /api/submissions/{id}` vẫn là nguồn sự thật.

## Năm thí nghiệm phân tán

| Thí nghiệm | Kết quả cần quan sát | Giải thích |
|---|---|---|
| Hai client SV001/SV002 chạy cùng lúc | Mỗi client chỉ đọc được thông báo lượt của mình | Server gửi vào group theo ID lượt nộp |
| Tắt API rồi mới chạy client | `StartAsync` báo lỗi kết nối | `WithAutomaticReconnect` chỉ thử lại sau khi kết nối đầu đã thành công |
| Sửa URL hub thành `/hub/submissions` | Negotiate lỗi 404 | Không khớp `MapHub` |
| Đổi tên On thành `SubmissionStatusChange` | Không báo lỗi, hết thời gian chờ | Tên event chiều server→client là hợp đồng chuỗi |
| Watch sau khi quá trình chấm xong | Không nhận tin cũ; GET REST trả `graded`/`failed` | Group không lưu lịch sử thông điệp |

Ghi kết quả chạy và thêm ảnh chụp terminal thật vào `anh/` trước khi nộp. Không dùng ảnh minh hoạ thay cho kết quả chạy.

## Câu hỏi ôn tập

- Polling lặp yêu cầu theo chu kỳ nên có yêu cầu thừa và độ trễ; push gửi khi có sự kiện.
- Yêu cầu–phản hồi do client khởi tạo, thường một cặp; push do sự kiện phía server kích hoạt, gửi thông điệp rời; streaming truyền liên tiếp nhiều thông điệp.
- WebSocket là giao thức hai chiều. SignalR là thư viện .NET có Hub, Group, quản lý kết nối và fallback qua WebSockets, Server-Sent Events, Long Polling.
- Group theo ID định tuyến đúng người nhận và tránh phát dữ liệu riêng tư của người khác.
- `IHubContext` gửi thông điệp từ bên ngoài Hub, như tác vụ nền.
- `On<T>` đăng ký bộ xử lý cho tên sự kiện server gửi về client.
- REST tạo và đọc tài nguyên với mã trạng thái rõ ràng; SignalR chỉ bổ sung thông báo thay đổi.
- `WithAutomaticReconnect` thử nối lại sau khi kết nối đã mở bị đứt. ConnectionId mới cần đăng ký group lại.
- Scale-out cần backplane/dịch vụ SignalR dùng chung vì group mặc định ở trong bộ nhớ từng tiến trình.
- Polling phù hợp khi dữ liệu ít đổi hoặc ứng dụng chấp nhận trễ.

## Nộp bài

Tạo ảnh chụp màn hình terminal có thư mục làm việc, lệnh và kết quả đọc được; lưu trong `anh/`. Theo hướng dẫn repo lớp, tạo PR từ fork với tiêu đề `Bai3 - dtc245200714 - Dương Thu Huyền`.

