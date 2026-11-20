# Nhật ký Bài 3 – EduSubmit realtime (RealtimeSubmissionSystem)

Thiết kế và kế hoạch 8 bước: xem `PingSignalR/docs/nhat-ky.md` (slide 42–47).

## Bước 1: tạo solution và hai project (slide 48)

| Lệnh | Kết quả |
|---|---|
| `dotnet new sln -n RealtimeSubmissionSystem` | `RealtimeSubmissionSystem.slnx` |
| `dotnet new web -n Submission.RealtimeApi -o src/Submission.RealtimeApi` | Máy chủ: REST Bài 2 + hub (template web) |
| `dotnet new console -n Submission.RealtimeClient -o src/Submission.RealtimeClient` | Máy khách: HttpClient + HubConnection |
| `dotnet sln add …` (2 lần) | `dotnet sln list` có đủ 2 project |
| `dotnet add src/Submission.RealtimeClient package Microsoft.AspNetCore.SignalR.Client --version 10.0.15` | csproj có `PackageReference … Version="10.0.15"` |
| `dotnet new gitignore`, `dotnet build` | **Build succeeded**, 0 Error(s) |

Ảnh: `rs-b1-tao-solution.png` (chạy các lệnh, build), `rs-b1-kiem-tra.png` (`ls src`, `dotnet sln list`, PackageReference).

## Bước 2: REST của Bài 2 + UpdatedAt + lock (slide 49)

Lấy nguyên phần REST của bài Bài 2 đã nộp (`Bai2/RestfulClientServer/src/Submission.Api/Program.cs`) — giáo trình nói code đầy đủ ở Phụ lục A tài liệu Word — rồi thêm hai điểm theo slide:

- `SubmissionDto` có thêm `DateTimeOffset UpdatedAt`: thời điểm đổi trạng thái gần nhất; khi tạo thì `ReceivedAt = UpdatedAt = DateTimeOffset.UtcNow`. Máy khách so sánh `UpdatedAt` để biết tin nào mới hơn.
- `var submissionsLock = new object();` và `lock (submissionsLock)` quanh `submissions.Add(...)` (POST) và `FirstOrDefault(...)` (GET theo id): ở các bước sau, tác vụ chấm nền và request HTTP cùng sửa danh sách, nên mỗi thời điểm chỉ một luồng được đọc/ghi.

Máy chủ chạy ở cổng 5000: `dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000`. Dữ liệu gửi đặt trong `tests/sub-hop-le.json`, `tests/sub-thieu.json`.

| Yêu cầu | Kết quả |
|---|---|
| `POST /api/assignments/1/submissions` hợp lệ | **201 Created**, `Location: /api/submissions/{id}`, body có `"status":"received"` và `"updatedAt"` ✓ (kiểm tra của slide) |
| `POST` vào bài tập 3 (đã đóng) | 400 `ASSIGNMENT_CLOSED` |
| `POST` thiếu mã sinh viên / kho mã nguồn | 400 `INVALID_SUBMISSION` |
| `GET /api/submissions/{id}` không tồn tại | 404 `SUBMISSION_NOT_FOUND` |

Ảnh: `rs-b2-may-chu.png`, `rs-b2-curl-rest.png`.
