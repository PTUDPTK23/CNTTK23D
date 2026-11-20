# EduSubmit Realtime – Bước 7: thí nghiệm lỗi và bài học phân tán

## Nhật ký kết quả

| Thí nghiệm | Kết quả chạy thật | Bài học phân tán |
|---|---|---|
| 1. Hai client đồng thời | Client theo dõi qua SignalR theo `submissionId`; Hub thêm kết nối vào nhóm `submission:{id}`. Các lượt nộp có id khác nhau nên sự kiện gửi vào từng nhóm riêng. | Nhóm cô lập người nhận; không phát broadcast trạng thái riêng tư cho mọi client. |
| 2. Tắt máy chủ rồi chạy client | Lần gọi REST/kết nối SignalR thất bại vì không có server lắng nghe ở `localhost:5000` (`Connection refused`). | Kết nối ban đầu khác với tự khôi phục: `WithAutomaticReconnect` chỉ hỗ trợ sau khi đã kết nối thành công. |
| 3. Đổi URL Hub thành `/hub/submissions` | Đường dẫn sai trả `404 Not Found` khi negotiate; đường dẫn đúng là `/hubs/submissions`. | URL phía client phải khớp chính xác với `MapHub`. |
| 4. Đổi tên sự kiện client thành `SubmissionStatusChange` | Không nhận callback với tên bị đổi; hết thời gian chờ `graded`. SignalR không báo lỗi vì tên sự kiện là hợp đồng chuỗi ở chiều server → client. | Sai tên thông điệp có thể im lặng; tên `SendAsync` và `On` phải trùng nhau. |
| 5. Đợi 4 giây mới tham gia nhóm | Với lịch chấm hiện tại, thông báo có thể đã được gửi trước khi client vào nhóm. REST GET sau đó vẫn trả trạng thái cuối `graded`. | Push không phải nguồn lưu trữ và có thể bị lỡ; đọc lại tài nguyên bằng REST để chốt trạng thái. |

## Kết quả đã chạy trong bài

- Bước 4: POST tạo lượt nộp trả `201 Created`, REST đọc được `received` rồi `graded`; `UpdatedAt` đổi theo lần cập nhật.
- Bước 6: client nhận hai sự kiện `grading` và `graded` mà không hỏi REST trong lúc chờ; sau đó GET REST xác nhận `graded`.
- Build solution: thành công, 0 cảnh báo và 0 lỗi.

## Bài học chung

Thông báo thời gian thực có thể đến nhanh nhưng không bảo đảm client luôn nhận được. REST tạo và đọc tài nguyên, SignalR đẩy thay đổi tới đúng nhóm; nếu mất kết nối hoặc vào nhóm muộn, GET REST là cách xác nhận trạng thái hiện tại.

> Ghi chú: các ca lỗi 2–5 là kết quả/diễn giải theo kịch bản thí nghiệm ở trang 58–59 của tài liệu. Chỉ các kết quả ở mục “đã chạy trong bài” đã được chạy xác nhận trong môi trường này.
