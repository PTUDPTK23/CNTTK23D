# Bài 3 – SignalR

![Hướng dẫn chụp ảnh kết quả theo từng bước](huong-dan-chup-anh.png)

## Bước 1.1 – Tạo solution và hai project PingSignalR

Tạo solution `PingSignalR.slnx` trong thư mục `PingSignalR`.

![Terminal báo tạo solution PingSignalR thành công](buoc1-build.png)

Tạo hai project `Ping.Api` và `Ping.Client`. Cả hai đều restore thành công. Ảnh chụp lần chạy từ thư mục `Bai3`. Sau đó hai project được chuyển vào `PingSignalR/src` theo cấu trúc của slide.

![Terminal báo tạo và restore hai project Ping thành công](buoc1.build1.png)

## Bước 1.2 – Thêm project vào solution và build

- Đã thêm Ping.Api và Ping.Client vào solution.
- Đã thêm gói Microsoft.AspNetCore.SignalR.Client phiên bản 10.0.15 cho Ping.Client và tạo .gitignore.
- Build thành công với 0 Warning(s), 0 Error(s). Lệnh kiểm tra là `dotnet build --no-restore` sau khi đã restore gói từ nuget.org.

![Tạo gitignore và build thành công](buoc1.build2.png)
