# Nhật ký thí nghiệm Bài 3

API chạy trên `http://localhost:5000`. Máy chủ mô phỏng chấm bằng `Task.Delay`: `processing` sau 1 giây, kết thúc sau 3 giây. Mỗi lượt nộp có GUID riêng.

| # | Thao tác | Kết quả chạy thật |
| --- | --- | --- |
| 1 | Chạy hai máy khách đồng thời với mã SV001 và SV002. | SV001 chỉ nhận `processing`, `graded` của `4512902c-73b9-4a87-aada-6a9a4a36d1f7`; SV002 chỉ nhận `processing`, `graded` của `a06af9b7-8dd4-4461-8f46-416388a4fb0b`. Không lẫn nhóm. |
| 2 | Khởi chạy client khi không có API nghe tại cổng 5999. | HTTP GET danh sách bài tập báo `Connection refused`; client chưa thể kết nối hub hoặc tạo submission. `WithAutomaticReconnect` áp dụng sau khi kết nối đã được thiết lập, không khởi động lại lần Start đầu. |
| 3 | POST negotiate tới `/hub/submissions/negotiate`. | `404`; đường dẫn đúng `/hubs/submissions` trả `200`. |
| 4 | Đổi event lắng nghe thành `SubmissionStatusChange`. | Client không nhận callback, không phát sinh lỗi SignalR; sau timeout, REST GET trả `graded` cho id `c3429931-76b0-42f1-929e-a085d8af2c6e`. |
| 5 | Trì hoãn `WatchSubmission` 4 giây bằng `--watch-delay=4`. | Không có sự kiện push nào được phát lại; REST GET trả `graded` cho id `028923b7-53a5-41a8-add8-3a60bc542ca9`. |

Thêm ca lỗi URL: nộp `http://example.com/repo` nhận `processing` rồi `failed`; client in cảnh báo và REST GET xác nhận `failed`. Đây là mô phỏng kiểm tra URL, không phải trình chấm repository thật.
