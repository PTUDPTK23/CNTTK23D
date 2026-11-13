# Bài 2 – RESTful API cho tương tác client-server
Họ tên: Hoàng Đức Hiếu            MSSV: dtc245200528           Lớp: CNTT K23D

## 1. Catalog API (Phần 2)

### Lệnh đã chạy

```powershell
mkdir CatalogApi
cd CatalogApi
dotnet new sln -n CatalogApi
dotnet new web -n Catalog.Api -o src/Catalog.Api
dotnet new console -n Catalog.Client -o src/Catalog.Client
dotnet sln add src/Catalog.Api/Catalog.Api.csproj
dotnet sln add src/Catalog.Client/Catalog.Client.csproj
dotnet build

# Terminal 1 - máy chủ
dotnet run --project src/Catalog.Api --urls http://localhost:5100

# Terminal 2 - gửi POST bằng curl, body lấy từ tệp JSON
curl.exe -i -X POST http://localhost:5100/api/products -H "Content-Type: application/json" -d "@sp-hop-le.json"
curl.exe -i -X POST http://localhost:5100/api/products -H "Content-Type: application/json" -d "@sp-sai.json"
curl.exe -i -X POST http://localhost:5100/api/products -d "@sp-hop-le.json"

# Terminal 2 - máy khách
dotnet run --project src/Catalog.Client
```

### Các endpoint

| Method | URI | Thành công | Lỗi |
|---|---|---|---|
| GET | `/` | 200 `{ server, status }` | – |
| GET | `/api/products` | 200 danh sách sản phẩm | – |
| GET | `/api/products/{id:int}` | 200 một sản phẩm | 404 `PRODUCT_NOT_FOUND` |
| POST | `/api/products` | 201 + Location | 400 `INVALID_PRODUCT` |

### Ảnh kết quả

**curl.exe: POST hợp lệ (201), POST sai (400), thiếu Content-Type (415)**
<img width="1920" height="1080" alt="Screenshot (19)" src="https://github.com/user-attachments/assets/1dadcb35-6047-4f62-b3cb-83eae11850a6" />
<img width="1920" height="1080" alt="Screenshot (20)" src="https://github.com/user-attachments/assets/e6b39b9d-cb6d-48e7-bec2-338201c6140b" />
<img width="1920" height="1080" alt="Screenshot (21)" src="https://github.com/user-attachments/assets/7b222313-aa6e-4313-9e2e-43cd784aa35e" />




**Máy khách: liệt kê → tạo mới → đọc chi tiết → gửi sai**
<img width="1920" height="1080" alt="Screenshot (22)" src="https://github.com/user-attachments/assets/36473b35-98b6-4248-8f59-f04ea4e0e91c" />


## 2. EduSubmit lát cắt 2 (Phần 3)

### Hợp đồng API

| Method | URI | Thành công | Lỗi có thể gặp |
|---|---|---|---|
| GET | `/api/assignments` | 200 danh sách | – |
| GET | `/api/assignments/{assignmentId}` | 200 một bài tập | 404 `ASSIGNMENT_NOT_FOUND` |
| POST | `/api/assignments/{assignmentId}/submissions` | 201 + Location | 404 `ASSIGNMENT_NOT_FOUND` · 400 `ASSIGNMENT_CLOSED` · 400 `INVALID_SUBMISSION` |
| GET | `/api/submissions/{submissionId}` | 200 lượt nộp | 404 `SUBMISSION_NOT_FOUND` |

Mọi lỗi do handler trả về cùng một dạng `{ "code": "...", "message": "..." }`.

### Lệnh đã chạy

```powershell
mkdir RestfulClientServer ; cd RestfulClientServer
dotnet new sln -n RestfulClientServer
dotnet new web -n Submission.Api -o src/Submission.Api
dotnet new console -n Submission.Client -o src/Submission.Client
dotnet sln add src/Submission.Api/Submission.Api.csproj
dotnet sln add src/Submission.Client/Submission.Client.csproj
dotnet new gitignore
dotnet build

# Terminal 1 - máy chủ
dotnet run --project src/Submission.Api --urls http://localhost:5000

# Terminal 2 - máy khách
dotnet run --project src/Submission.Client -- http://localhost:5000
```

### Ảnh kết quả theo từng bước

**Bước 2: `curl.exe -i http://localhost:5000/` → 200 OK**

<img width="1920" height="1080" alt="Screenshot (23)" src="https://github.com/user-attachments/assets/2deb30ca-1d31-402b-bbee-666702b3fb4f" />


**Bước 3: `GET /api/assignments` → 200 + mảng JSON**

<img width="1920" height="1080" alt="Screenshot (24)" src="https://github.com/user-attachments/assets/45b1cba4-300b-479b-a6cf-78f5cf6c6b50" />


**Bước 4: `/api/assignments/1` → 200, `/999` → 404 có ApiError, `/abc` → 404 body rỗng**

<img width="1920" height="1080" alt="Screenshot (25)" src="https://github.com/user-attachments/assets/19d5601f-71b9-44ed-8b42-b75ce91ea42d" />
<img width="1920" height="1080" alt="Screenshot (26)" src="https://github.com/user-attachments/assets/dec2a9fc-66b2-44a1-8a43-317cd892e8c9" />
<img width="1920" height="1080" alt="Screenshot (27)" src="https://github.com/user-attachments/assets/f1fd037d-2547-4b6e-9ec9-2bf8e2fa55bd" />


**Bước 5: POST nộp bài → 201 + Location**

<img width="1920" height="1080" alt="Screenshot (28)" src="https://github.com/user-attachments/assets/faafd468-f52c-4c87-82a1-39da7d0093b4" />


**Bước 6: GET lại theo Location → 200**

<img width="1920" height="1080" alt="Screenshot (29)" src="https://github.com/user-attachments/assets/7f0486c8-524e-4100-ba9f-dc415c9ffc97" />


**Bước 7: máy khách chạy đủ 4 nấc**

(dán ảnh)

**Bước 8: `requests.http` đủ 7 request (200, 201, 400, 404)**

(dán ảnh)

## 3. Bảng phá thử (8 ca ở bước 5)

| Ca | Yêu cầu | Dự đoán | Thực tế | Ai trả | Giải thích |
|---|---|---|---|---|---|
| 5a | POST bài 1, body `sv001.json` | 201 + Location | | Handler | Tạo thành công, có địa chỉ để đọc lại |
| 5b | POST bài 1, body `rong.json` | 400 `INVALID_SUBMISSION` | | Handler | Máy khách gửi thiếu dữ liệu |
| 5c | POST bài 999, body `sv001.json` | 404 `ASSIGNMENT_NOT_FOUND` | | Handler | Tài nguyên cha không tồn tại |
| 5d | Như 5a nhưng bỏ `-H "Content-Type..."` | 415, body rỗng | | Framework | Không biết body là JSON nên hàm xử lý không chạy |
| 5e | Body JSON hỏng `{"studentCode":"SV001",` | 400, body văn bản | | Framework | Không đọc được body |
| 5f | Body thiếu `repositoryUrl` | 400 `INVALID_SUBMISSION` | | Handler | Trường thiếu thành null, handler bắt được |
| 5g | `PUT /api/assignments/1` | 405, `Allow: GET` | | Framework | URI có nhưng phương thức không được hỗ trợ |
| 5h | Đặt bài 2 `IsOpen = false`, POST vào bài 2 | 400 `ASSIGNMENT_CLOSED` | | Handler | Đúng cú pháp nhưng sai quy tắc nghiệp vụ |

## 4. Trả lời 3 câu hỏi

**Câu 1. Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?**

URI chỉ dùng để định danh tài nguyên, ví dụ `assignments` và `submissions`. Hành động trên tài nguyên đó do phương thức HTTP thể hiện: GET để đọc, POST để tạo, DELETE để xóa. Nhờ vậy cùng một URI có thể dùng cho nhiều thao tác mà không cần đặt thêm tên mới. Ví dụ, nộp bài viết là `POST /api/assignments/1/submissions`, nghĩa là "tạo một lượt nộp thuộc bài tập 1", chứ không viết `POST /submitAssignment`. Cách này giúp API thống nhất và dễ đoán. Các phương thức còn mang sẵn ngữ nghĩa chuẩn của HTTP: GET an toàn và lũy đẳng, POST thì không.

**Câu 2. "Stateless" thể hiện ở đâu trong code đã viết?**

Mỗi yêu cầu tự mang đủ thông tin để máy chủ xử lý. `assignmentId` nằm trên URI, còn `studentCode` và `repositoryUrl` nằm trong body của `POST /api/assignments/{assignmentId}/submissions`. Máy chủ không nhớ phiên hay trạng thái của máy khách giữa hai lần gọi. Ví dụ, khi đọc lại lượt nộp, máy khách phải tự gửi id qua `GET /api/submissions/{submissionId}` (lấy từ header Location). "Stateless" không có nghĩa là máy chủ không lưu dữ liệu: danh sách `submissions` vẫn được lưu trong bộ nhớ. Ý nghĩa là máy chủ không giữ ngữ cảnh hội thoại của từng máy khách.

**Câu 3. Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?**

DTO là hợp đồng JSON, không phải một lớp dùng chung. Máy chủ chuyển `SubmissionDto` thành JSON để gửi đi, máy khách đọc JSON đó vào record `SubmissionDto` do chính nó khai báo. Hai bên khớp nhau vì có cùng tên trường và kiểu dữ liệu trong JSON, nên không cần tham chiếu project của nhau. Nhờ vậy client và server độc lập: có thể sửa phần bên trong của một bên, thậm chí viết client bằng ngôn ngữ khác, miễn là vẫn giữ đúng hợp đồng. Đổi lại, nếu đổi tên trường ở một bên mà quên đổi ở bên kia thì lỗi chỉ lộ ra khi chạy, ví dụ máy khách in ra tên trống.

## 5. Luồng thông điệp

```
Client --(GET  /api/assignments)------------------> Api : 200 + danh sách bài tập
Client --(POST /api/assignments/1/submissions)----> Api : 201 Created + Location (sai thì 400/404)
Client --(GET  /api/submissions/{id})-------------> Api : 200 lượt nộp (không có thì 404)
Client --(POST dữ liệu sai)-----------------------> Api : 400 + ApiError
```

## 6. Khó khăn gặp phải và cách giải quyết

| Khó khăn | Nguyên nhân | Cách giải quyết |
|---|---|---|
| Project API bị đặt tên `Catalop.Api`, client nằm trong thư mục `sec/` | Gõ nhầm tên khi tạo project | Xóa 2 project vừa tạo rồi tạo lại đúng tên `Catalog.Api`, `src/Catalog.Client` |
| `curl: option -i-X: is unknown` | Thiếu dấu cách giữa `-i` và `-X` | Gõ lại `curl.exe -i -X POST ...` |
| `curl: Failed to open sp-hop-le.json` | Chưa tạo tệp JSON ở thư mục đang đứng | Tạo `sp-hop-le.json`, `sp-sai.json` ở thư mục gốc `CatalogApi` |
| Build báo lỗi `Newline in constant` ở máy khách | Chép code từ slide, chuỗi `$"..."` bị xuống dòng giữa chừng | Viết chuỗi trên một dòng |
| `The provided file path does not exist: src/Catalog.Api--urls` | Thiếu dấu cách trước `--urls` | Gõ lại `dotnet run --project src/Catalog.Api --urls http://localhost:5100` |
| Build báo `file is locked by: Catalog.Api` | API cũ vẫn đang chạy | `Ctrl+C` ở terminal cũ rồi chạy lại |
