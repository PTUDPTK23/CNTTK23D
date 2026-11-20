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
