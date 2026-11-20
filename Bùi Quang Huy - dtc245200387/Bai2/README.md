# EduSubmit – Bài 2 RESTful API

**Sinh viên:** Bùi Quang Huy  
**Mã sinh viên:** DTC245200387  
**Lớp:** CNTTK23D  
**Nền tảng:** .NET 10 / ASP.NET Core Minimal API, HttpClient  
**Kho mã nguồn cá nhân:** [https://github.com/buiquanghuy0623/CNTTK23D](https://github.com/buiquanghuy0623/CNTTK23D)  

---

## 1. Cấu trúc thư mục

```text
Bai2/
├── .gitignore
├── EduSubmit.sln
├── README.md
├── requests.http
├── requests-extension.http
├── rong.json
├── sv001.json
├── docs/
│   └── images/
├── src/
│   ├── Submission.Api/
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Program.cs
│   │   ├── Submission.Api.csproj
│   │   └── Properties/
│   │       └── launchSettings.json
│   └── Submission.Client/
│       ├── Program.cs
│       └── Submission.Client.csproj
└── CatalogApi/
    ├── .gitignore
    ├── CatalogApi.slnx
    ├── requests.http
    ├── sp-hop-le.json
    ├── sp-sai.json
    └── src/
        ├── Catalog.Api/
        └── Catalog.Client/
```

---

## 2. Hướng dẫn Build solution

Mở terminal tại thư mục `Bai2`:

```powershell
dotnet restore .\EduSubmit.sln
dotnet build .\EduSubmit.sln
```

Kết quả mong đợi: `Build succeeded, 0 Warning(s), 0 Error(s)`.

---

## 3. Khởi động máy chủ API

Tại terminal thứ nhất, khởi động máy chủ API tại cổng 5000:

```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Kiểm tra API đang hoạt động:

```powershell
curl.exe -i http://localhost:5000/
curl.exe -i http://localhost:5000/api/assignments
```

*Lưu ý:* Dữ liệu được lưu trữ trong bộ nhớ (RAM), do đó khi khởi động lại API danh sách lượt nộp sẽ được làm mới.

---

## 4. Chạy chương trình Console Client

Mở terminal thứ hai tại thư mục `Bai2` và chạy client:

```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

Client thực hiện đầy đủ 4 nấc kiểm thử:
1. **Nấc 1:** Lấy và in danh sách các bài tập đang mở (`IsOpen == true`).
2. **Nấc 2:** Gửi yêu cầu `POST` tạo lượt nộp bài hợp lệ, nhận mã trạng thái `201 Created` và đọc header `Location`.
3. **Nấc 3:** Đọc lại thông tin lượt nộp bài qua URI nhận được từ header `Location` (`GET /api/submissions/{id}`) với kết quả `200 OK`.
4. **Nấc 4:** Cố ý gửi dữ liệu không hợp lệ (`studentCode` và `repositoryUrl` rỗng), nhận mã `400 Bad Request` cùng đối tượng `ApiError`, chương trình xử lý lỗi an toàn không bị dừng đột ngột.

---

## 5. Hợp đồng API (API Contract)

| Phương thức | URI | Trạng thái thành công | Lỗi có thể trả về | Mô tả |
|---|---|---|---|---|
| `GET` | `/` | `200 OK` | — | Kiểm tra trạng thái máy chủ (`health check`) |
| `GET` | `/api/assignments` | `200 OK` | — | Lấy danh sách toàn bộ bài tập |
| `GET` | `/api/assignments/{id:int}` | `200 OK` | `404 Not Found` | Xem chi tiết bài tập theo ID |
| `POST` | `/api/assignments/{id:int}/submissions` | `201 Created` | `400 Bad Request`<br>`404 Not Found`<br>`409 Conflict`<br>`415 Unsupported Media Type` | Nộp bài tập; phản hồi chứa header `Location` trỏ tới URI lượt nộp |
| `GET` | `/api/submissions/{id:guid}` | `200 OK` | `404 Not Found` | Đọc lại thông tin một lượt nộp |
| `DELETE`| `/api/submissions/{id:guid}` | `204 No Content`| `404 Not Found` | Xóa một lượt nộp (phục vụ ca kiểm thử mở rộng) |

Cấu trúc đối tượng lỗi chuẩn (`ApiError`):
```json
{
  "code": "INVALID_SUBMISSION",
  "message": "Thieu ma sinh vien hoac kho ma nguon."
}
```

---

## 6. Danh mục 7 Request trong `requests.http`

Tệp `requests.http` bao gồm đúng 7 request chuẩn tương thích với tiện ích REST Client trên VS Code:

1. **Request 1 (200 OK):** `GET /` — Kiểm tra trạng thái máy chủ.
2. **Request 2 (200 OK):** `GET /api/assignments` — Lấy danh sách các bài tập.
3. **Request 3 (200 OK):** `GET /api/assignments/1` — Lấy chi tiết bài tập số 1.
4. **Request 4 (404 Not Found):** `GET /api/assignments/999` — Kiểm tra bài tập không tồn tại (`ASSIGNMENT_NOT_FOUND`).
5. **Request 5 (201 Created):** `POST /api/assignments/1/submissions` — Nộp bài tập hợp lệ, sinh mã `201 Created` và header `Location`.
6. **Request 6 (200 OK):** `GET /api/submissions/{{createSubmission.response.body.$.id}}` — Đọc lại lượt nộp bài vừa tạo ở Request 5 theo dynamic ID.
7. **Request 7 (400 Bad Request):** `POST /api/assignments/1/submissions` — Gửi dữ liệu rỗng, kiểm tra lỗi xác thực (`INVALID_SUBMISSION`).

---

## 7. Nhật ký kiểm tra (Bảng phá thử theo slide hướng dẫn)

| Bước / Ca | Thử nghiệm | Phương thức & URI | Mã HTTP mong đợi | Nơi xử lý | Kết quả quan sát thực tế |
|---|---|---|:---:|---|---|
| **1** | Build toàn bộ solution | `dotnet build` | — | MSBuild | Build succeeded, 0 warning, 0 error |
| **2** | Kiểm tra dịch vụ hoạt động | `GET /` | `200 OK` | Handler | `{"service":"Submission.Api","status":"running"}` |
| **3** | Danh sách bài tập | `GET /api/assignments` | `200 OK` | Handler | Mảng JSON gồm 2 bài tập |
| **4** | Đọc bài tập tồn tại & không tồn tại | `GET /api/assignments/1`<br>`GET /api/assignments/999`<br>`GET /api/assignments/abc` | `200 OK`<br>`404 Not Found`<br>`404 Not Found` | Handler<br>Handler<br>Framework | Trả về bài tập 1;<br>`404 ASSIGNMENT_NOT_FOUND`;<br>`404` rỗng do route constraint `:int` không khớp |
| **5a** | Nộp bài hợp lệ vào bài tập 1 | `POST /api/assignments/1/submissions` (body đầy đủ) | `201 Created` | Handler | Nhận `201 Created`, header `Location: /api/submissions/{guid}` |
| **5b** | Nộp bài với body rỗng | `POST /api/assignments/1/submissions` (body `{}`) | `400 Bad Request` | Handler | Trả về `400 INVALID_SUBMISSION` |
| **5c** | Nộp bài vào bài tập không tồn tại | `POST /api/assignments/999/submissions` | `404 Not Found` | Handler | Trả về `404 ASSIGNMENT_NOT_FOUND` |
| **5d** | Gửi JSON thiếu `Content-Type` | `POST /api/assignments/1/submissions` (không header) | `415 Unsupported Media Type` | Framework | Máy chủ từ chối tiếp nhận vì không nhận dạng được kiểu nội dung |
| **5e** | Gửi JSON sai cú pháp | `POST /api/assignments/1/submissions` (JSON lỗi cú pháp) | `400 Bad Request` | Framework | Framework báo lỗi phân tích cú pháp JSON |
| **5f** | Thiếu trường `repositoryUrl` | `POST /api/assignments/1/submissions` (`{"studentCode":"SV001"}`) | `400 Bad Request` | Handler | Trả về `400 INVALID_SUBMISSION` |
| **5g** | Sai phương thức HTTP | `PUT /api/assignments/1` | `405 Method Not Allowed` | Framework | Header trả về `Allow: GET` |
| **5h** | Nộp vào bài tập đã đóng | `POST /api/assignments/2/submissions` (bài 2 đóng) | `400 Bad Request` | Handler | Trả về `400 ASSIGNMENT_CLOSED` |
| **6** | Đọc lại qua `Location` | `GET /api/submissions/{guid}` | `200 OK` | Handler | Nhận thông tin lượt nộp vừa tạo; thử ID sai trả `404 SUBMISSION_NOT_FOUND` |
| **7** | Chạy Client tự động | `dotnet run --project src/Submission.Client` | — | Console | In danh sách, nộp bài, đọc lại qua Location, bắt lỗi 400 và in thông báo lỗi |
| **8** | Đếm request `requests.http` | Đếm trong tệp | — | REST Client | Đủ 7 request bao phủ các mã trạng thái 200, 201, 400, 404 |
| **9 (Mở rộng)** | Gửi trùng, DELETE, GET sau DELETE | `POST`, `DELETE`, `GET` | `409 Conflict`<br>`204 No Content`<br>`404 Not Found` | Handler | Trả `409 DUPLICATE_SUBMISSION`; xóa trả `204`; đọc lại trả `404 SUBMISSION_NOT_FOUND` |

---

## 8. Trả lời 3 câu hỏi lý thuyết

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Nguyên lý thiết kế REST:** URI đóng vai trò định danh tài nguyên (*Resource Identifier*), do đó cần sử dụng danh từ (ví dụ: `/api/assignments`, `/api/submissions`).
- **Phân định rõ trách nhiệm:** Hành động thực hiện lên tài nguyên thuộc về giao thức HTTP thông qua các phương thức chuẩn (*Verbs*): `GET` (đọc dữ liệu), `POST` (tạo mới), `PUT/PATCH` (cập nhật), `DELETE` (xóa).
- **Lợi ích:** Tránh việc tạo ra các URI mang tính hành động rườm rà như `/createSubmission` hay `/getAssignmentById`, giúp cấu trúc API nhất quán, dễ mở rộng và tuân thủ đúng chuẩn RESTful.

### Câu 2: Tính chất "Stateless" thể hiện ở đâu trong ứng dụng?
- **Trong từng yêu cầu:** Mỗi yêu cầu gửi từ client lên server đều tự mang đầy đủ ngữ cảnh và dữ liệu cần thiết để xử lý độc lập: `assignmentId` nằm trên URI, `studentCode` và `repositoryUrl` nằm trong thân JSON body.
- **Phía server:** Server không duy trì bất kỳ phiên làm việc (*session state*) nào giữa các lần gọi liên tiếp của client. Hai request gửi từ cùng một client được xử lý hoàn toàn độc lập, server không cần nhớ trạng thái của request trước đó để phục vụ request tiếp theo.

### Câu 3: Vì sao client và server khai báo DTO riêng mà vẫn khớp nhau?
- **Tính độc lập của tiến trình:** Project Client và project API là hai chương trình tách biệt, chạy ở hai tiến trình riêng biệt và không trực tiếp tham chiếu mã nguồn của nhau.
- **Hợp đồng dữ liệu (Contract):** Hai bên giao tiếp thông qua giao thức HTTP với định dạng trung gian là chuỗi JSON. Miễn là các trường thông tin trong DTO ở cả hai phía có cùng tên gọi (ví dụ: `StudentCode`, `RepositoryUrl`) và kiểu dữ liệu tương thích, trình tuần hoàn hóa (*Serializer/Deserializer*) sẽ tự động chuyển đổi qua lại chính xác mà không cần chia sẻ chung thư viện lớp C#.

---

## 9. Ảnh minh chứng kết quả chạy

Các ảnh chụp màn hình kiểm thử được lưu trữ tại thư mục `docs/images/`:
- `docs/images/01-build.png`: Màn hình lệnh `dotnet build EduSubmit.sln` thành công.
- `docs/images/02-api-running.png`: Màn hình khởi động `Submission.Api` tại cổng 5000 và lệnh gọi `curl`.
- `docs/images/03-client-running.png`: Màn hình chạy `Submission.Client` qua đủ 4 nấc kiểm thử.
- `docs/images/04-requests-http.png`: Màn hình kiểm thử 7 request qua REST Client trong VS Code.
