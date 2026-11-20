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

## Bước 5: máy khách mở hai kênh (slide 55)

- Kênh 1 REST: `HttpClient { BaseAddress = http://localhost:5000 }`. Kênh 2 SignalR: `HubConnectionBuilder().WithUrl(".../hubs/submissions").WithAutomaticReconnect()`. Một URL gốc cho cả hai.
- `On<SubmissionStatusMessage>("SubmissionStatusChanged", …)` đăng ký **trước** `StartAsync`; SignalR tự chuyển JSON thành record (record khai báo giống máy chủ ở cuối tệp).
- `TaskCompletionSource gradedNotification`: "cái chuông", hoàn tất khi nhận `graded` (`TrySetResult`).

Kiểm tra: máy chủ đang chạy, máy khách in `Da ket noi SignalR.` ✓ — ảnh `rs-b5-may-chu.png`, `rs-b5-client-ket-noi.png`.

## Bước 6: tạo bằng REST, theo dõi bằng SignalR, chốt bằng REST (slide 56–57)

Thứ tự trong máy khách: ① `GET /api/assignments` → ② `POST …/submissions` (kiểm tra 201 trước khi đọc body, lỗi thì in `ApiError`) → ③ `InvokeAsync("WatchSubmission", id)` → ④ `Task.WhenAny(chuông graded, 10 giây)` → ⑤ `GET /api/submissions/{id}` đọc lại. Máy khách tự thoát sau khi nhận `graded`.

**Kết quả máy khách (8 dòng)** – `dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV001`:

```text
Da ket noi SignalR.
Chon bai tap: 1 - Bai tap REST API
Da tao luot nop: 020c4d58-118a-44b6-ac71-5802d310473e
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: 020c4d58-118a-44b6-ac71-5802d310473e -> processing luc 09:32:23
Thong bao SignalR: 020c4d58-118a-44b6-ac71-5802d310473e -> graded luc 09:32:25
Trang thai doc lai bang REST: graded
```

**Máy chủ:** `[Hub] sr3n_2uM theo doi submission:020c4d58…` → `[Push] … -> processing` → `[Push] … -> graded`.

Kiểm tra: đủ 8 dòng, 2 dòng "Thong bao SignalR" cách nhau 2 giây ✓. Hai cập nhật đến từ máy chủ mà máy khách không hề hỏi. Ảnh: `rs-b6-client-sv001.png`, `rs-b6-may-chu.png`.

## Bước 7: năm thí nghiệm phân tán (slide 58–60)

Dự đoán ghi **trước** khi chạy (slide 58), sau đó chạy và so sánh. Sau mỗi thí nghiệm, máy khách được trả về đúng mã nguồn ban đầu.

| # | Thí nghiệm | Dự đoán | Kết quả thật | Giải thích | Ảnh |
|---|---|---|---|---|---|
| 1 | Chạy 2 máy khách SV001 và SV002 **cùng lúc** | Mỗi máy khách chỉ nhận thông báo của lượt nộp mình tạo | Máy chủ in xen kẽ 2 lượt nộp (`90347484…`, `70ad97f8…`); cửa sổ SV001 chỉ có `70ad97f8… -> processing/graded`, SV002 chỉ có lượt nộp của SV002 | Mỗi máy khách vào nhóm `submission:{id}` riêng; `Clients.Group` tách người nhận nên không trộn lẫn | `rs-b7-tn1-may-chu.png`, `rs-b7-tn1-sv001.png`, `rs-b7-tn1-sv002.png` |
| 2 | Chạy máy khách khi **máy chủ chưa chạy** | Lỗi kết nối, chương trình dừng | `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5000)` | `StartAsync` gọi negotiate qua HTTP, không có tiến trình nghe cổng 5000. `WithAutomaticReconnect` không thử lại lần nối đầu tiên | `rs-b7-tn2-may-chu-chua-chay.png` |
| 3 | Sửa `WithUrl` thành `/hub/submissions` | Lỗi 404 | `HttpRequestException: Response status code does not indicate success: 404 (Not Found)` tại `NegotiateAsync` | Lỗi ở bước negotiate; đường dẫn phải khớp `MapHub("/hubs/submissions")` | `rs-b7-tn3-sai-duong-dan.png` |
| 4 | Sửa `On("SubmissionStatusChanged")` thành `On("SubmissionStatusChange")` | Không lỗi, không có dòng "Thong bao SignalR" | Không lỗi nào; 10 giây sau in `Qua thoi gian cho thong bao graded.`; REST đọc lại vẫn `graded` | Máy chủ vẫn gửi `SubmissionStatusChanged` nhưng không có hàm nào nghe tên đó → bị bỏ qua im lặng. Sai tên chiều xuống = im lặng | `rs-b7-tn4-sai-ten-su-kien.png` |
| 5 | Chờ 4 giây rồi mới gọi `WatchSubmission` | Lỡ cả 2 thông báo, REST vẫn `graded` | Không nhận tin nào; `Qua thoi gian cho thong bao graded.`; `Trang thai doc lai bang REST: graded` | Chấm xong sau ~3 giây, trước khi vào nhóm; SignalR không lưu tin cho người vào nhóm muộn. REST là nguồn sự thật | `rs-b7-tn5-watch-muon.png` |

**Bài học:** thông điệp thời gian thực có thể bị lỡ; thiết kế đúng là **push để nhanh + REST để chắc**. Trật tự đúng ở máy khách (slide 60): **nghe (`On`) → nối (`StartAsync`) → tạo (`POST`) → theo dõi (`WatchSubmission`) → đọc lại (`GET`)**. Không nhận được thông báo thì kiểm tra theo thứ tự: cổng → đường dẫn hub → Watch đúng id sau POST → tên thông điệp.

## Bước 8: requests.http – 9 ca (slide 61)

`requests.http` (extension REST Client) gom 9 ca; để chụp minh chứng, `tests/chay-9-ca.ps1` chạy đúng 9 ca đó bằng `curl.exe` và in mã HTTP.

| Ca | Yêu cầu | Mong đợi | Thực tế |
|---|---|---|---|
| 1 | `GET /` | 200 | 200 |
| 2 | `GET /api/assignments` | 200 | 200 |
| 3 | `POST /api/assignments/1/submissions` hợp lệ | 201 + Location, `received` | 201, `received` |
| 4 | `GET /api/submissions/{id}` | 200; bấm lại thấy trạng thái đổi | 200; sau 4 giây `graded` |
| 5 | `POST` thiếu `studentCode`, `repositoryUrl` | 400 | 400 |
| 6 | `POST /api/assignments/99/submissions` | 404 | 404 |
| 7 | `GET /api/submissions/0000…` | 404 | 404 |
| 8 | `POST /hubs/submissions/negotiate` | 200 | 200 |
| 9 | `POST /hub/submissions/negotiate` (sai) | 404 | 404 |

Cả 9 ca đúng mã mong đợi ✓ — ảnh `rs-b8-may-chu.png`, `rs-b8-requests-9-ca.png`.
