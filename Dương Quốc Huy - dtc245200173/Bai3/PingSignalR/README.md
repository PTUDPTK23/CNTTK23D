# PingSignalR — Bài 3

**Dương Quốc Huy — dtc245200173**

## Cách chạy

Cần .NET SDK 10. Máy khách dùng gói `Microsoft.AspNetCore.SignalR.Client` phiên bản `10.0.15`.

Từ thư mục `PingSignalR`, build cả hai project:

```powershell
dotnet build
```

Chạy mỗi lệnh sau trong một terminal riêng, theo thứ tự từ trên xuống. Để An kết nối xong trước khi chạy Binh. Nhấn Enter ở từng máy khách để thoát.

```powershell
dotnet run --project src/Ping.Api --urls http://localhost:5200
dotnet run --project src/Ping.Client -- http://localhost:5200 An
dotnet run --project src/Ping.Client -- http://localhost:5200 Binh
```

Kiểm tra bước bắt tay:

```powershell
curl.exe -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"
```

## Kết quả chạy

Build thành công với 0 cảnh báo và 0 lỗi. `GET /` trả 200. Bắt tay trả 200, có `connectionId` và ba cơ chế truyền tải WebSockets, ServerSentEvents, LongPolling. Gọi `GET /hubs/ping` trực tiếp khi thiếu mã kết nối trả 400.

Trích đầu ra của An khi hai máy khách cùng kết nối, sau đó khởi động lại máy chủ:

```text
[Echo] Echo: Xin chao SignalR tu An
[Group] Thong bao nhom tu An
[Broadcast] Xin chao tat ca tu An
[Group] Thong bao nhom tu Binh
[Broadcast] Xin chao tat ca tu Binh
[Lifecycle] Dang ket noi lai...
[Lifecycle] Da ket noi lai: ek8Sw5YTIrnGHhXCPM_Syg
[Group] ek8Sw5YT da tham gia nhom lop-hoc-01
```

Echo của Binh chỉ xuất hiện ở terminal Binh (`Clients.Caller`). Tin nhóm xuất hiện ở các kết nối thuộc `lop-hoc-01` (`Clients.Group`). Tin phát rộng đến mọi kết nối đang mở (`Clients.All`). Sau khi máy chủ khởi động lại, An nhận mã kết nối mới và tự gia nhập lại nhóm.

## Thí nghiệm lỗi

| Thao tác | Dự đoán | Kết quả thực tế | Giải thích |
| --- | --- | --- | --- |
| Chạy máy khách khi API đã tắt | Mở kết nối thất bại | `HttpRequestException`, localhost:5200 từ chối kết nối | `WithAutomaticReconnect` áp dụng sau khi kết nối ban đầu đã mở thành công. |
| Gửi POST negotiate tới `/hub/ping` | Trả 404 | HTTP 404 | Đường dẫn đã ánh xạ là `/hubs/ping`. |
| Tạm đổi tên đăng ký từ `EchoResponse` thành `EchoRespone` rồi chạy máy khách | Echo biến mất, các sự kiện khác vẫn đến | Group và Broadcast vẫn in, chương trình thoát bình thường, không có dòng Echo | Tên sự kiện bên gửi và bên nghe phải khớp. Tên đúng đã được khôi phục sau khi thử. |
