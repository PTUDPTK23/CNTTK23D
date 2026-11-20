# RestfulClientServer - Hệ thống nộp bài thực hành (EduSubmit)

Ứng dụng minh họa kiến trúc Client-Server chuẩn RESTful gồm:
- `src/Submission.Api`: Web API đóng vai trò Máy chủ (Server) quản lý bài tập và tiếp nhận lượt nộp.
- `src/Submission.Client`: Ứng dụng Console đóng vai trò Máy khách (Client) giao tiếp với API qua `HttpClient`.

## 1. Cách chạy chương trình

### Bước 1: Khởi động Máy chủ (Submission.Api)
Mở Terminal tại thư mục `RestfulClientServer` và chạy:
```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

### Bước 2: Chạy Máy khách (Submission.Client)
Mở một Terminal thứ hai tại thư mục `RestfulClientServer` và chạy:
```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

## 2. Danh sách các Endpoint

| Phương thức | Đường dẫn | Ý nghĩa | Mã trạng thái |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/assignments` | Lấy danh sách bài tập | `200 OK` |
| `GET` | `/api/assignments/{assignmentId}` | Xem chi tiết một bài tập | `200 OK`, `404 Not Found` |
| `POST` | `/api/assignments/{assignmentId}/submissions` | Nộp bài tập | `201 Created` (+ `Location`), `400 Bad Request`, `404 Not Found` |
| `GET` | `/api/submissions/{submissionId}` | Đọc lại thông tin lượt nộp | `200 OK`, `404 Not Found` |

## 3. Lược đồ JSON

**Yêu cầu nộp bài (`CreateSubmissionRequest`):**
```json
{
  "studentCode": "SV001",
  "repositoryUrl": "[https://github.com/sv001/rest-api](https://github.com/sv001/rest-api)"
}
```

**Phản hồi lượt nộp (`SubmissionDto`):**
```json
{
  "id": "48971cd9-7760-42de-8321-a59c9c25741b",
  "assignmentId": 1,
  "studentCode": "SV001",
  "repositoryUrl": "[https://github.com/sv001/rest-api](https://github.com/sv001/rest-api)",
  "status": "received",
  "receivedAt": "2026-11-13T02:34:00Z"
}
```

**Phản hồi lỗi thống nhất (`ApiError`):**
```json
{
  "code": "INVALID_SUBMISSION",
  "message": "Thieu ma sinh vien hoac kho ma nguon."
}
```