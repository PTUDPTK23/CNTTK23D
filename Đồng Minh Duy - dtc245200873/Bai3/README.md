# Bài 3 - SignalR

Hai solution trong thư mục này:

- `PingSignalR`: Echo, broadcast và group trên cổng 5200.
- `RealtimeSubmissionSystem`: REST tạo/đọc submission, SignalR đẩy trạng thái `processing` và `graded` theo nhóm riêng.

## Chạy

```text
dotnet run --project PingSignalR/src/Ping.Api --urls http://localhost:5200
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 SV001
dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeApi --urls http://localhost:5000
dotnet run --project RealtimeSubmissionSystem/src/Submission.RealtimeClient -- http://localhost:5000 SV001
```

Kết quả EduSubmit: kết nối SignalR, tạo lượt nộp `received`, nhận `processing`, nhận `graded`, rồi đọc lại REST là `graded`.

Minh chứng EduSubmit: log kết quả chạy thật trong `RealtimeSubmissionSystem/anh/edusubmit-result.txt`; ảnh chụp màn hình cần chụp trực tiếp từ terminal sau khi chạy trên máy người học.

## Trả lời câu hỏi

Máy chủ chủ động báo qua kết nối lâu dài SignalR và `IHubContext`; client không cần polling.
REST tạo và đọc tài nguyên với mã trạng thái rõ ràng; SignalR chỉ đẩy thay đổi phát sinh sau đó.
Nếu mất tin `graded`, kết nối lại có thể có `ConnectionId` mới; client vào lại nhóm và gọi GET REST để lấy trạng thái cuối.

## Năm thí nghiệm

1. Hai client cùng lúc: mỗi client chỉ nhận submission của mình; nhóm theo id giữ riêng tư và giảm băng thông.
2. Máy chủ tắt: client báo lỗi kết nối/refused; lần nối đầu không tự thử lại.
3. Sai `/hub/submissions`: negotiate/StartAsync nhận 404 vì đường dẫn không khớp `MapHub`.
4. Sai tên event: không có exception nhưng không nhận push; hết thời gian chờ rồi REST vẫn đọc được trạng thái.
5. Gọi Watch muộn: bỏ lỡ tin đã phát; REST vẫn là nguồn sự thật và trả `graded`.

## Thiết kế

Server cập nhật danh sách trong lock trước, sau đó gửi message tối thiểu gồm id, status và thời điểm tới `submission:{id}`. SignalR giúp biết sớm; REST đảm bảo đọc được kết quả cuối.

## Bài tập mở rộng

- Repository không bắt đầu bằng `https://` sẽ chuyển trạng thái cuối thành `failed`; client in cảnh báo màu đỏ.
- Client tạo hai lượt nộp trên cùng một kết nối, đăng ký một `TaskCompletionSource` cho mỗi id và gọi `WatchSubmission` cho cả hai. Kết quả mẫu: lượt HTTPS là `graded`, lượt HTTP là `failed`; các thông báo không trộn lẫn vì mỗi lượt dùng một group riêng.
