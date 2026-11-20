# Bài 3 – SignalR

## Bước 1.1 – Tạo solution và hai project

Tạo solution PingSignalR, project ASP.NET Core Ping.Api và project console Ping.Client.

![Tạo solution PingSignalR](anh/buoc1-tao-solution.png)

![Tạo Ping.Api và Ping.Client](anh/buoc1-tao-project.png)

## Bước 1.2 – Thêm hai project vào solution và build

Cả hai project có trong solution. Máy khách dùng Microsoft.AspNetCore.SignalR.Client 10.0.15. File .gitignore bỏ qua thư mục bin và obj.

    dotnet build

Kết quả kiểm tra: build thành công, 0 cảnh báo, 0 lỗi.

![Kết quả dotnet build](anh/buoc1-build.png)
