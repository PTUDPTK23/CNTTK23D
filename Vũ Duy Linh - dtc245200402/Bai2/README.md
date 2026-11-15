# Bài 2: Thiết kế và Hiện thực RESTful API cho Tương tác Client - Server

**Họ và tên:** Vũ Duy Linh  
**Mã sinh viên:** dtc245200402  
**Lớp:** CNTT K23D  
**Kho mã nguồn:** [https://github.com/dtc245200402-bit/V-Duy-Linh-dtc245200402](https://github.com/dtc245200402-bit/V-Duy-Linh-dtc245200402)

---

## 1. Cấu trúc bài nộp

Thư mục `Vũ Duy Linh - dtc245200402/Bai2/` bao gồm 2 phần thực hành theo đúng hướng dẫn:

- **`CatalogApi/`**: Dự án thực hành thiết kế Catalog API (cổng `5100`):
  - `src/Catalog.Api`: Minimal API quản lý danh mục sản phẩm (hỗ trợ GET, POST, ràng buộc route, xử lý mã 200, 201, 400, 404, 415).
  - `src/Catalog.Client`: Console app gọi Catalog API bằng `HttpClient`.
  - `requests.http`: Bộ kịch bản kiểm thử các endpoint Catalog.
- **`RestfulClientServer/`**: Dự án chính EduSubmit - Hệ thống nộp bài phân tán (cổng `5000`):
  - `src/Submission.Api`: Máy chủ Minimal API quản lý bài tập và lượt nộp bài.
  - `src/Submission.Client`: Máy khách Console nộp bài tự động, đọc lại lượt nộp và xử lý lỗi phản hồi.
  - `requests.http`: Bộ kiểm thử với hơn 7 requests bao phủ đầy đủ mã trạng thái HTTP.
- **`requests.http`**: Bộ kiểm thử chung tại thư mục gốc Bài 2 (gọi EduSubmit cổng `5000`).
- **`.gitignore`**: Loại bỏ hoàn toàn các thư mục `bin/`, `obj/`, `.vs/`.

---

## 2. Hướng dẫn chạy ứng dụng

### 2.1. Chạy hệ thống EduSubmit (RestfulClientServer - Cổng 5000)

1. **Khởi chạy máy chủ API:**
   Mở Terminal 1 tại thư mục `RestfulClientServer`:
   ```powershell
   cd "Vũ Duy Linh - dtc245200402/Bai2/RestfulClientServer"
   dotnet build
   dotnet run --project src/Submission.Api --urls http://localhost:5000
   ```
   Máy chủ hiển thị: `Now listening on: http://localhost:5000`.

2. **Khởi chạy máy khách Client:**
   Mở Terminal 2 tại thư mục `RestfulClientServer`:
   ```powershell
   dotnet run --project src/Submission.Client -- http://localhost:5000
   ```

3. **Kiểm tra nhanh bằng curl:**
   ```powershell
   curl.exe -i http://localhost:5000/
   curl.exe -i http://localhost:5000/api/assignments
   ```

### 2.2. Chạy ứng dụng Catalog API (CatalogApi - Cổng 5100)

1. **Khởi chạy máy chủ Catalog API:**
   Mở Terminal tại `CatalogApi`:
   ```powershell
   cd "Vũ Duy Linh - dtc245200402/Bai2/CatalogApi"
   dotnet build
   dotnet run --project src/Catalog.Api --urls http://localhost:5100
   ```

2. **Khởi chạy Catalog Client:**
   Mở Terminal thứ hai:
   ```powershell
   dotnet run --project src/Catalog.Client -- http://localhost:5100
   ```

---

## 3. Nhật ký các bước thực hiện

| Bước | Nội dung công việc | Kết quả đạt được |
|---|---|---|
| **Bước 1** | Khởi tạo Solution và 2 project độc lập | Tạo `RestfulClientServer.slnx`, `Submission.Api` (Web), `Submission.Client` (Console). Build thành công 0 warning, 0 error. |
| **Bước 2** | Hiện thực khung API cơ bản và kiểm tra sức khỏe | Cố định API tại cổng `5000`. Endpoint `GET /` trả về mã `200 OK` kèm `{"service":"Submission.Api","status":"running"}`. |
| **Bước 3** | Danh sách bài tập | Endpoint `GET /api/assignments` trả về danh sách bài tập kèm trạng thái mở/đóng (`IsOpen`). |
| **Bước 4** | Xem chi tiết bài tập | `GET /api/assignments/{id:int}`: Trả về `200 OK` nếu tìm thấy, trả về `404 Not Found` kèm `ASSIGNMENT_NOT_FOUND` nếu không tồn tại; route ràng buộc `:int` trả 404 cho tham số chữ cái. |
| **Bước 5** | Tiếp nhận lượt nộp bài | `POST /api/assignments/{id}/submissions`: Kiểm tra bài tập tồn tại, còn mở, body hợp lệ. Trả về `201 Created` kèm header `Location` trỏ tới `/api/submissions/{id}`. Trả `400` nếu thiếu dữ liệu hoặc bài tập đã đóng. |
| **Bước 6** | Đọc lại lượt nộp theo ID | `GET /api/submissions/{id:guid}`: Trả về `200 OK` cùng thông tin lượt nộp tương ứng. Trả `404 SUBMISSION_NOT_FOUND` nếu ID không có trong bộ nhớ. |
| **Bước 7** | Xây dựng Console Client hoàn chỉnh | Client thực hiện đủ 4 nấc: Lấy danh sách bài tập mở -> Nộp bài -> Đọc lại qua `Location` -> Cố ý gửi payload rỗng để nhận `400 Bad Request` mà không bị sập ứng dụng. |
| **Bước 8** | Bộ kiểm thử kịch bản `requests.http` | Xây dựng đầy đủ 7 request bắt buộc và các ca ngoại lệ mở rộng, bao phủ các mã HTTP: 200, 201, 400, 404, 405, 415. |

---

## 4. Bộ kiểm thử kịch bản requests.http

File `requests.http` đã được thiết kế đầy đủ các ca kiểm thử theo yêu cầu:

| STT | Phương thức & Tuyến đường | Dữ liệu gửi lên | Mã HTTP mong đợi | Mã thực tế | Ý nghĩa & Giải thích |
|---|---|---|---|---|---|
| 1 | `GET /` | Không | 200 OK | 200 OK | Máy chủ hoạt động bình thường (Health check). |
| 2 | `GET /api/assignments` | Không | 200 OK | 200 OK | Đọc danh sách bài tập dạng mảng JSON. |
| 3 | `GET /api/assignments/1` | Không | 200 OK | 200 OK | Tìm thấy bài tập ID = 1. |
| 4 | `GET /api/assignments/999` | Không | 404 Not Found | 404 Not Found | Bài tập không tồn tại, trả mã lỗi `ASSIGNMENT_NOT_FOUND`. |
| 5 | `POST /api/assignments/1/submissions` | `{"studentCode":"dtc245200402","repositoryUrl":"https://github.com/..."}` | 201 Created | 201 Created | Nộp bài thành công, máy chủ sinh Guid và trả header `Location`. |
| 6 | `GET /api/submissions/{id}` | Lấy `id` từ kết quả POST bước 5 | 200 OK | 200 OK | Đọc lại chính xác bản ghi vừa tạo qua `Location`. |
| 7 | `POST /api/assignments/1/submissions` | `{"studentCode":"","repositoryUrl":""}` | 400 Bad Request | 400 Bad Request | Dữ liệu rỗng, vi phạm validation, trả lỗi `INVALID_SUBMISSION`. |
| 8 | `POST /api/assignments/2/submissions` | `{"studentCode":"dtc245200402",...}` | 400 Bad Request | 400 Bad Request | Bài tập số 2 đã đóng (`IsOpen = false`), trả `ASSIGNMENT_CLOSED`. |
| 9 | `GET /api/submissions/00000000-...` | Guid không tồn tại | 404 Not Found | 404 Not Found | Không tìm thấy lượt nộp, trả `SUBMISSION_NOT_FOUND`. |
| 10 | `POST /api/assignments/1/submissions` | Thiếu Header `Content-Type` | 415 Unsupported Media Type | 415 Unsupported Media Type | Máy chủ từ chối vì không nhận diện được định dạng JSON của request body. |
| 11 | `PUT /api/assignments/1` | `{}` | 405 Method Not Allowed | 405 Method Not Allowed | Tuyến đường tồn tại nhưng phương thức PUT không được hỗ trợ. |

---

## 5. Thí nghiệm hệ phân tán (Bảng phá thử)

| # | Thí nghiệm | Dự đoán trước khi chạy | Kết quả thực tế | Đối chiếu | Giải thích nguyên nhân |
|---|---|---|---|---|---|
| **a** | Chạy Client lần thứ hai khi API vẫn đang hoạt động | Client vẫn lấy được danh sách bài tập, tạo thêm một lượt nộp mới với Guid ngẫu nhiên khác. | Client in danh sách bài tập, tạo thành công lượt nộp với Guid mới, đọc lại thành công. | **Đúng** | Trạng thái ứng dụng được quản lý tập trung ở Server; Client chỉ gửi yêu cầu độc lập theo mô hình Stateless. |
| **b** | Tắt API (Ctrl+C) rồi chạy Client | Client không kết nối được tới server, ném ngoại lệ kết nối mạng chứ không nhận được mã HTTP. | Xuất hiện ngoại lệ `HttpRequestException`: `No connection could be made because the target machine actively refused it (localhost:5000)`. Client bắt được lỗi và in thông báo, không bị sập. | **Đúng** | Trong hệ phân tán, máy chủ ở xa có thể ngừng hoạt động bất kỳ lúc nào. Đây là lỗi kết nối mạng (Network Level), không phải phản hồi mã HTTP từ ứng dụng. |
| **c** | Khởi động lại API và GET theo Guid cũ | Các lượt nộp cũ đã tạo trước đó sẽ biến mất và GET theo Guid cũ sẽ trả về 404. | Gửi request `GET /api/submissions/{Guid_cũ}` nhận mã `404 Not Found` kèm `SUBMISSION_NOT_FOUND`. | **Đúng** | Dữ liệu lượt nộp hiện tại được lưu tạm trong bộ nhớ RAM (`List<SubmissionDto>`), khi tiến trình máy chủ tắt thì toàn bộ dữ liệu trong bộ nhớ bị xóa. |
| **d** | Cho Client gọi tới cổng sai (cổng 5999) | Client báo lỗi không thể kết nối đến `localhost:5999` nhưng chương trình không bị crash đột ngột. | Client in thông báo `Loi ket noi API (http://localhost:5999): No connection could be made...` và kết thúc an toàn. | **Đúng** | Địa chỉ dịch vụ là tham số cấu hình mạng. Việc bọc khối `try-catch` quanh `HttpRequestException` giúp Client chống chịu lỗi hạ tầng mạng. |

---

## 6. Trả lời 3 câu hỏi bắt buộc

### 6.1. Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **URI (Uniform Resource Identifier)** có nhiệm vụ duy nhất là xác định danh tính của **tài nguyên** (Resource). Danh từ thể hiện rõ bản chất của tài nguyên đó trong hệ thống (ví dụ: `/api/assignments`, `/api/submissions`).
- **Phương thức HTTP (HTTP Verbs)** là giao thức chuẩn hóa các **hành động thao tác** trên tài nguyên: `GET` (đọc), `POST` (tạo mới), `PUT` (cập nhật toàn phần), `PATCH` (cập nhật từng phần), `DELETE` (xóa).
- **Lợi ích:**
  - Giữ cho URI trong sáng, phân cấp logic và không bị bùng nổ số lượng endpoint theo từng biến thể hành động (như `/getAssignments`, `/createSubmission`, `/removeSubmission`...).
  - Cho phép các thiết bị trung gian (Proxy, Cache, CDN) hiểu ngữ nghĩa của request mà không cần phân tích nội dung bên trong URI. Ví dụ `GET` là safe/idempotent nên có thể cache được, trong khi `POST` thì không.

### 6.2. “Stateless” thể hiện ở đâu trong code đã viết?
- **Khái niệm Stateless (Phi trạng thái):** Mỗi request gửi từ Client lên Server phải chứa đầy đủ mọi thông tin cần thiết để Server có thể hiểu và xử lý độc lập, Server không lưu lại bất kỳ trạng thái hội thoại (Session/Context) nào của Client giữa các lần gọi liên tiếp.
- **Biểu hiện cụ thể trong code:**
  - Mọi request tạo bài nộp đều gửi kèm `assignmentId` trên đường dẫn URL và toàn bộ payload (`StudentCode`, `RepositoryUrl`) trong body JSON.
  - Server không cần biết trước đó Client đã gọi những trang nào, đã đăng nhập hay duyệt bài nào.
  - Sau khi Server phản hồi `201 Created`, liên kết duy nhất để Client tìm lại tài nguyên là định danh toàn cục `Location: /api/submissions/{id}` chứa ID của chính lượt nộp.
  - *Lưu ý:* Stateless không có nghĩa là Server không được lưu dữ liệu. Danh sách `assignments` và `submissions` trên Server là dữ liệu tài nguyên nghiệp vụ (Resource State), hoàn toàn khác với trạng thái phiên tương tác của người dùng (Session State).

### 6.3. Vì sao Client và Server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Tính độc lập và lỏng lẻo (Loose Coupling):** Hai tiến trình không chia sẻ mã nguồn trực tiếp (không dùng `ProjectReference` để tránh phụ thuộc chặt chẽ vào nhau).
- **Hợp đồng dữ liệu (API Contract):** Hai bên thống nhất với nhau qua định dạng dữ liệu chuẩn `JSON` (JavaScript Object Notation) qua đường truyền HTTP.
- **Cơ chế tuần tự hóa (Serialization):** Bộ thư viện `System.Text.Json` chuyển đổi các thuộc tính của DTO thành chuỗi JSON ở phía gửi, và phục hồi chuỗi JSON thành thuộc tính tương ứng của DTO ở phía nhận dựa trên sự tương đồng về tên trường (Field Name) và kiểu dữ liệu (Data Type). Nhờ đó, Client và Server có thể được viết bằng các công nghệ khác nhau hoặc thay đổi cấu trúc nội bộ độc lập mà hệ thống vẫn tương thích.

---

## 7. Khó khăn gặp phải và giải pháp

1. **Vấn đề thư mục bin/obj bị commit vào Git:**
   - *Nguyên nhân:* Khi chạy lệnh build hoặc dotnet run, thư mục `bin/` và `obj/` tự động được sinh ra. Nếu không cấu hình `.gitignore` trước khi commit, Git sẽ theo dõi toàn bộ các tệp nhị phân này.
   - *Giải pháp:* Thiết lập file `.gitignore` chuẩn cho .NET, chạy lệnh `git rm -r --cached` để hủy theo dõi toàn bộ thư mục `bin/` và `obj/` mà không làm mất tệp nguồn.
2. **Xử lý phản hồi lỗi từ HttpClient phía Client:**
   - *Nguyên nhân:* Gọi `GetFromJsonAsync` trực tiếp khi API trả về mã lỗi 4xx sẽ làm Client ném ngoại lệ và có nguy cơ văng ứng dụng nếu không xử lý.
   - *Giải pháp:* Sử dụng `PostAsJsonAsync` hoặc `GetAsync`, kiểm tra `response.StatusCode` hoặc `response.IsSuccessStatusCode` trước. Nếu thất bại, đọc nội dung lỗi vào `ApiError` record để in ra màn hình một cách an toàn.

---

## 8. Danh mục kiểm tra trước khi nộp (Checklist)

- [x] Toàn bộ mã nguồn nằm đúng trong thư mục `Vũ Duy Linh - dtc245200402/Bai2/`.
- [x] Không còn bất kỳ file bài làm nào nằm rải rác ngoài thư mục gốc repo.
- [x] Không đưa các thư mục `bin/` và `obj/` lên Git.
- [x] File `requests.http` có đầy đủ 7 request bắt buộc và bao phủ 200, 201, 400, 404, 405, 415.
- [x] Solution `CatalogApi.slnx` và `RestfulClientServer.slnx` build thành công với 0 warning, 0 error.
- [x] `README.md` trình bày đầy đủ lệnh chạy, bảng thí nghiệm phá thử và 3 câu trả lời lý thuyết.
