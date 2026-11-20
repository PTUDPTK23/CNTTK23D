# Bài 3 — SignalR

**Đồng Anh Tuấn · dtc245200427 · CNTT K23D**

Cần .NET 10 SDK. Các lệnh bên dưới chạy từ thư mục của từng solution.

## Phần 1 — PingSignalR

```powershell
dotnet restore --source https://api.nuget.org/v3/index.json
dotnet build --no-restore
dotnet run --project src/Ping.Api --no-build -- --urls http://localhost:5200
```

Ở terminal thứ hai, chạy An. Giữ An mở rồi chạy Bình ở terminal thứ ba.

```powershell
dotnet run --project src/Ping.Client --no-build -- http://localhost:5200 An
dotnet run --project src/Ping.Client --no-build -- http://localhost:5200 Binh
```

An nhận tin nhóm và tin phát rộng của Bình. Echo chỉ gửi về client vừa gọi. Xem [nhật ký thử lỗi và kết nối lại](PingSignalR/src/Ping.Client/NhatKy-Bai3.md).

## Phần 2 — RealtimeSubmissionSystem

Client tạo lượt nộp bằng REST, nhận cập nhật processing và graded qua SignalR, rồi đọc trạng thái cuối bằng REST. Xem [hướng dẫn chạy và kết quả năm thí nghiệm](RealtimeSubmissionSystem/NhatKy.md) và [chín ca HTTP](RealtimeSubmissionSystem/requests.http).
