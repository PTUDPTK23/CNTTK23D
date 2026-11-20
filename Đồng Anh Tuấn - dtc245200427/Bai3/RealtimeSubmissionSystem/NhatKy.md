# Nhật ký RealtimeSubmissionSystem

## Chạy lại

Trong thư mục RealtimeSubmissionSystem, mở hai terminal. Lần đầu cần tải các gói NuGet:

```powershell
dotnet restore --source https://api.nuget.org/v3/index.json
dotnet build --no-restore
```

Terminal 1 chạy máy chủ:

```powershell
dotnet run --project src/Submission.RealtimeApi --no-build -- --urls http://localhost:5000
```

Terminal 2 chạy client:

```powershell
dotnet run --project src/Submission.RealtimeClient --no-build -- http://localhost:5000 SV001
```

Dữ liệu lưu trong bộ nhớ. Khởi động lại API sẽ xóa các lượt nộp của lần chạy trước.

## Kiểm tra luồng chính

- Cả hai project build thành công với 0 cảnh báo và 0 lỗi.
- POST lượt nộp trả 201, có Location và trạng thái received. Ba lần GET sau đó lần lượt trả received, processing và graded. UpdatedAt tăng qua cả ba lần đọc.
- Hai thông báo SignalR processing và graded đến cách nhau 2 giây trong lần chạy thử. Client đọc lại REST và xác nhận graded.

## Bước 7 — Năm thí nghiệm

Các thí nghiệm đều chạy với API thử ở http://localhost:5000. Ba biến thể client được build từ bản sao để phục vụ thí nghiệm.

| Thao tác | Dự đoán | Kết quả quan sát | Giải thích |
| --- | --- | --- | --- |
| Chạy hai client cùng lúc | Mỗi client nhận tin của lượt nộp mình tạo. | SV001 nhận processing và graded cho 2a0534de-c21c-49ad-9626-91fa06b907da. SV002 nhận cả hai cho 77b36f28-2e1e-4a01-a121-cca97a5643de. Không có tin của lượt nộp kia trong hai đầu ra. | Mỗi lượt nộp có nhóm SignalR riêng. |
| Chạy client trước khi mở API | Kết nối ban đầu bị từ chối. | HttpRequestException báo máy đích từ chối kết nối tại localhost:5000. | WithAutomaticReconnect áp dụng khi kết nối đã thiết lập rồi bị mất. |
| Dùng /hub/submissions thay cho /hubs/submissions | Bước bắt tay trả 404. | HttpRequestException với 404 (Not Found). | URL client phải khớp đường dẫn MapHub. |
| Nghe SubmissionStatusChange thay cho SubmissionStatusChanged | Client chờ hết 10 giây rồi đọc trạng thái bằng REST. | Không có thông báo SignalR. Client báo hết thời gian chờ và REST trả graded. | Tên sự kiện phía nhận phải khớp tên phía gửi. |
| Đợi 4 giây sau POST rồi mới gọi WatchSubmission | Client bỏ lỡ cả hai thông báo, REST vẫn trả kết quả. | Không có thông báo SignalR. Client báo hết thời gian chờ và REST trả graded. | Thông báo đã gửi trước khi client vào nhóm. REST đọc dữ liệu mới nhất. |

## Bước 8 — Kết quả chín ca trong requests.http

| Ca | Mã thực tế |
| --- | --- |
| 1. GET / | 200 |
| 2. GET danh sách bài tập | 200 |
| 3. POST hợp lệ | 201 |
| 4. GET lượt nộp | 200 |
| 5. POST thiếu dữ liệu | 400 |
| 6. POST bài 99 | 404 |
| 7. GET GUID rỗng | 404 |
| 8. POST negotiate đúng tuyến | 200, có connectionId |
| 9. POST negotiate sai tuyến | 404 |

Ca bổ sung: POST vào bài đã đóng trả 400 với mã ASSIGNMENT_CLOSED.
