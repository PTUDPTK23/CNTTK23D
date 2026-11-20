# Hệ thống Nộp bài & Cập nhật Trạng thái Realtime (EduSubmit)

## 1. Kiến trúc hệ thống
- **REST API (`Submission.RealtimeApi` - cổng 5000):** Đóng vai trò là nguồn dữ liệu chuẩn (source of truth), xử lý tạo lượt nộp (`POST /api/assignments/{id}/submissions`) và truy vấn trạng thái bài nộp (`GET /api/submissions/{id}`).
- **SignalR Hub (`/hubs/submissions`):** Đẩy sự kiện cập nhật trạng thái (`SubmissionStatusChanged`) từ Server xuống Client theo thời gian thực khi tiến trình nền chuyển trạng thái `received` -> `processing` -> `graded`.
- **Cơ chế phân nhóm (`submission:{id}`):** Mỗi Client gọi `WatchSubmission(submissionId)` để tham gia đúng nhóm theo dõi bài nộp của mình, đảm bảo thông báo không gửi nhầm sang sinh viên khác.

## 2. Hướng dẫn chạy
1. Khởi động máy chủ:
   ```bash
   dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000