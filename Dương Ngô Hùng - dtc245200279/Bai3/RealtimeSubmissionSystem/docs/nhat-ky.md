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

## Bước 3: SubmissionHub và nhóm submission:{id} (slide 50–51)

- `builder.Services.AddSignalR()` trước `Build()`; `app.MapHub<SubmissionHub>("/hubs/submissions")` trước `Run()`.
- `SubmissionHub.WatchSubmission(id)`: chỉ thêm kết nối vào nhóm `submission:{id}` (hub mỏng, không tạo/chấm bài) và in `[Hub] … theo doi submission:{id}`.
- `SubmissionGroups.ForSubmission(...)`: một chỗ duy nhất sinh tên nhóm để hub và tác vụ nền không lệch tên.

| Kiểm tra | Kết quả |
|---|---|
| `POST /hubs/submissions/negotiate?negotiateVersion=1` | 200, có `connectionId` và 3 transport ✓ |
| `POST /hub/submissions/negotiate?negotiateVersion=1` (thiếu `s`) | **404 Not Found** ✓ |

Ảnh: `rs-b3-may-chu.png`, `rs-b3-negotiate.png`.

**Vì sao nhóm theo lượt nộp (slide 51):** ① **riêng tư** – chỉ người theo dõi đúng lượt nộp mới nhận trạng thái, máy khách không tự lọc nên không bao giờ nhìn thấy dữ liệu của sinh viên khác; ② **băng thông** – một lượt nộp đổi trạng thái chỉ gửi cho vài kết nối trong nhóm thay vì gửi cho mọi kết nối (`Clients.All`) rồi để máy khách bỏ đi.

## Bước 4: IHubContext và mô phỏng chấm nền (slide 52–54)

- `POST` nhận thêm `IHubContext<SubmissionHub> hubContext` (ASP.NET Core đưa vào bằng DI) và gọi `_ = SimulateGradingAsync(...)` **không chờ**: POST trả 201 ngay, chấm bài chạy tiếp phía sau.
- `SimulateGradingAsync`: `Task.Delay` 1 giây → `processing`, 2 giây → `graded`.
- `UpdateStatusAsync`: ① `lock` rồi thay phần tử bằng `with { Status, UpdatedAt }` (record bất biến) → ② tạo `SubmissionStatusMessage(Id, Status, UpdatedAt)` → ③ `hub.Clients.Group("submission:{id}").SendAsync("SubmissionStatusChanged", msg)` và in `[Push] …`. **Lưu trước, đẩy sau**: ai đọc REST ngay khi nhận tin đều thấy đúng.
- Không truyền đối tượng hub vào tác vụ nền được (hub chỉ sống trong một lời gọi) → luôn dùng `IHubContext` khi gửi từ ngoài hub.

| Lần GET | Trạng thái | updatedAt |
|---|---|---|
| Ngay sau POST | `received` | 02:29:49.80 |
| Sau ~1,5 giây | `processing` | 02:29:50.81 |
| Sau ~4 giây | `graded` | 02:29:52.82 |

Đúng thứ tự received → processing → graded ✓; máy chủ in `[Push] submission:… -> processing`, `-> graded`. REST luôn biết trạng thái đúng; SignalR chỉ giúp máy khách biết sớm hơn. Ảnh: `rs-b4-may-chu.png`, `rs-b4-rest-doi-trang-thai.png`.
