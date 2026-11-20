# Kết quả build và endpoint

## Build

Đã build hai solution với .NET SDK 10.0.403:

| Solution | Kết quả |
| --- | --- |
| `PingSignal/PingSignal.slnx` | Build succeeded, 0 warning, 0 error. |
| `RealtimeSubmissionSystem/RealtimeSubmissionSystem.slnx` | Build succeeded, 0 error. Có một cảnh báo `NU1900` do môi trường không phân giải được `api.nuget.org` để lấy dữ liệu audit lỗ hổng; các gói cần thiết đã được restore. |

Lệnh dùng trong workspace:

```powershell
dotnet build PingSignal/PingSignal.slnx --no-restore -m:1
dotnet build RealtimeSubmissionSystem/RealtimeSubmissionSystem.slnx --no-restore -m:1
```

## Chạy máy khách

- `PingSignal`: client kết nối thành công và in `Echo: Xin chào SignalR`.
- `RealtimeSubmissionSystem`: client in đúng chu trình REST `received`, push `processing`, push `graded`, rồi GET REST xác nhận `graded`.

## Chín ca HTTP

Chạy trên `http://localhost:5000` theo thứ tự trong [requests.http](../../RealtimeSubmissionSystem/requests.http). Mã nhận được:

```text
200, 200, 201, 200, 400, 404, 404, 200, 404
```

Lần GET ca 4 phải dùng id vừa tạo ở ca 3 để thấy `received`, `processing` hoặc `graded`; GUID số 0 ở ca 7 là ca không tồn tại.
