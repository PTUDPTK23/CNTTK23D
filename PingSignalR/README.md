# PingSignalR

Bài tập SignalR với ASP.NET Core — `Ping.Api` (server hub) và `Ping.Client` (console client).

## Bước 1: build

![buoc1-build](anh/buoc1-build.png)

Build cả 2 project thành công, 0 Error(s).

```bash
cd PingSignalR
dotnet build src/Ping.Api/Ping.Api.csproj
dotnet build src/Ping.Client/Ping.Client.csproj
```

## Bước 2: negotiate

![buoc2-negotiate](anh/buoc2-negotiate.png)

Máy chủ trả về `connectionId` và danh sách transport (WebSockets, ServerSentEvents, LongPolling).

```bash
Invoke-RestMethod -Method POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"
```

## Bước 3–4: Echo

![buoc4-echo](anh/buoc4-echo.png)

Máy chủ chạy, máy khách gọi `Echo` và nhận `[Echo] Echo: Xin chao SignalR`.

```bash
# Terminal 1
dotnet run --project src/Ping.Api --urls http://localhost:5200

# Terminal 2
dotnet run --project src/Ping.Client -- http://localhost:5200 An
```

## Bước 5: Nhóm (Group)

![buoc5-nhom](anh/buoc5-nhom.png)

Hai máy khách (An, Binh) cùng join nhóm `lop-hoc-01` — thấy tin `[Group]` và `[Broadcast]` của nhau, không thấy tin `[Echo]` riêng của nhau.

## Bước 6: Tự kết nối lại

![buoc6-noilai](anh/buoc6-noilai.png)

`WithAutomaticReconnect()` — khi mất kết nối in _"Đang kết nối lại..."_, sau khi server khởi động lại in _"Đã kết nối lại"_.

## Bước 7: Lỗi cố ý

| Ảnh | Lỗi |
|-----|-----|
| ![loi1](anh/buoc7-loi1.png) | Lỗi 1 |
| ![loi2](anh/buoc7-loi2.png) | Lỗi 2 |
| ![loi3](anh/buoc7-loi3.png) | Lỗi 3 |

## EduSubmit

![edusubmit](anh/edusubmit-graded.png)

Kết quả requests.http (200, 201, 400, 404) và máy khách in graded.

---

> **Lưu ý:** thư mục `bin/` và `obj/` không được commit (đã có trong `.gitignore`).
