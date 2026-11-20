# PingSignalR

Chạy máy chủ:

```text
dotnet run --project src/Ping.Api --urls http://localhost:5200
```

Chạy client:

```text
dotnet run --project src/Ping.Client -- http://localhost:5200 SV001
```

Client hỗ trợ `echo`, `broadcast`, `join`, `group`, `leave` và `exit`. Ảnh minh chứng nằm trong thư mục `anh/`.
