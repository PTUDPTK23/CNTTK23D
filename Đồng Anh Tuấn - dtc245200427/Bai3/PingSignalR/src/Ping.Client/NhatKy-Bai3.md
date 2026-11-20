# Nhật ký Bài 3 — Thử sai tên SignalR

Máy chủ đang chạy tại http://localhost:5200.

| Lần thử | Thay đổi ở Ping.Client | Kết quả thực tế |
| --- | --- | --- |
| 1. Máy khách gọi hub | Gọi `Ech0` thay cho `Echo` | `HubException: Failed to invoke 'Ech0' due to an error on the server. HubException: Method does not exist.` |
| 2. Hub gửi máy khách | Nghe `EchoRespone` thay cho `EchoResponse` | Client kết nối và gọi Echo thành công nhưng không in dòng `[Echo]`. |
| 3. Khôi phục tên đúng | Gọi `Echo` và nghe `EchoResponse` | `[Echo] Echo: Xin chao SignalR tu Tuan` |

Lần 1 tạm bắt ngoại lệ để in nguyên nhân. Lần 2 chờ thêm 600 ms để quan sát phản hồi. Sau hai lần thử, đã khôi phục Program.cs và build lại với 0 lỗi.

## Bước 6 — Ngắt và kết nối lại

Chạy API thử trên cổng 5201 và kết nối client An. Dừng API khi client còn mở rồi chạy lại. Client in:

```text
[System] Client mwrYTemI da ket noi.
[Lifecycle] Dang ket noi lai...
[Lifecycle] Da ket noi lai: zG3Yww3M
```

Mã kết nối mới khác mã cũ. Nhóm gắn với kết nối cũ, nên client cần gọi JoinGroup lại nếu muốn tiếp tục nhận tin nhóm sau khi kết nối lại.

## Bước 7 — Ba thí nghiệm lỗi

Thực hiện trên bản thử riêng ở cổng 5201.

| Làm gì | Thấy gì | Vì sao |
| --- | --- | --- |
| Chạy client khi API chưa mở | `HttpRequestException`. Máy đích từ chối kết nối trên localhost:5201. | `StartAsync()` không mở được kết nối ban đầu. Tự kết nối lại chỉ áp dụng sau khi từng kết nối thành công. |
| Đổi đường dẫn client thành `/hub/ping` | `HttpRequestException` kèm `404 (Not Found)`. | Bước negotiate gọi nhầm tuyến. Máy chủ ánh xạ hub ở `/hubs/ping`. |
| Đăng ký nghe `EchoRespone` | Client kết nối và nhận tin nhóm, tin phát rộng. Không có dòng `[Echo]` và không báo lỗi. | Máy chủ gửi tên `EchoResponse`, còn client đăng ký nghe tên khác. |

Chạy lại với URL và tên thông điệp đúng, client in `[Echo] Echo: Xin chao SignalR tu Tuan`.
