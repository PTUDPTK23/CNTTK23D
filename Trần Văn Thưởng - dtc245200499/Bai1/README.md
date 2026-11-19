# Đề 20 – Hệ thống thư viện và đặt mượn sách phân tán

Mẫu triển khai theo hướng dẫn Bài 1: hai tiến trình độc lập giao tiếp qua HTTP. `Submission.Api` quản lý danh mục và yêu cầu đặt mượn trong bộ nhớ; `Submission.Client` truy vấn sách, tạo một yêu cầu hợp lệ rồi đọc lại yêu cầu theo GUID. API có lọc danh mục, kiểm tra đầu vào, trả 201/400/404/409 và giảm số bản còn sẵn khi nhận đặt mượn.

## Chạy

Cần .NET 10 SDK. Mở hai terminal tại thư mục solution này.

Terminal 1:

```powershell
dotnet build
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Terminal 2:

```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

Thử các tình huống HTTP bằng REST Client trong VS Code, mở `requests.http`. Yêu cầu đặt mượn và số lượng bản còn được lưu trong bộ nhớ, vì vậy khởi động lại API sẽ khôi phục dữ liệu mẫu.

## Luồng thông điệp

```text
Client --(HTTP GET /api/books)--------> API : request-response (200)
Client --(HTTP POST /api/reservations)-> API : request-response (201 hoặc 400/409)
Client --(HTTP GET /api/reservations/{id})-> API : request-response (200/404)
```

Hai project không tham chiếu code của nhau; hợp đồng JSON được biểu diễn bằng các record tương ứng ở mỗi phía.

## Nội dung gói

- `src/Submission.Api`: Minimal API và dữ liệu mẫu.
- `src/Submission.Client`: console client dùng `HttpClient`.
- `requests.http`: kiểm thử 200, 201, 400, 404, 409.
- `docs/bai1.md`: nhật ký theo mẫu bài hướng dẫn, gồm trả lời và bảng thí nghiệm cần hoàn thiện bằng kết quả thực hành thực tế.


