# Bài 1 – Tổng quan hệ thống và MEP
Họ tên:  Hoàng Đức Hiếu            MSSV:   dtc245200528           Lớp:cntt k23d
 
## 1. Cài đặt
<img width="1920" height="1080" alt="Screenshot (2)" src="https://github.com/user-attachments/assets/cf690aee-a33a-4585-8521-9ae9f76aa7ba" />


 
## 2. MepDemo (Bước 7)
- Trả lời 3 câu hỏi; mỗi MEP 1 câu mô tả
 
## 3. BasicWebApi (Bước 8)
- Ảnh chụp REST Client: 200, 201, 400
 <img width="1920" height="1080" alt="Screenshot (3)" src="https://github.com/user-attachments/assets/5f3faa19-0141-4266-9be1-873bee8ad043" />
 <img width="1920" height="1080" alt="Screenshot (4)" src="https://github.com/user-attachments/assets/67fddf47-cde5-4bcb-a405-7a7428883dae" />
<img width="1920" height="1080" alt="Screenshot (5)" src="https://github.com/user-attachments/assets/b908ec0e-8270-42e9-8a13-4292a34ef859" />
<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/5eb3c402-d13f-46bf-aeed-a24ffec8190a" />
<img width="1920" height="1080" alt="Screenshot (6)" src="https://github.com/user-attachments/assets/d7b1d57f-e463-4992-b666-1e00004fcb0d" />
Câu 1 	Hai dòng builder.Services.AddSingleton… và var app = builder.Build(); – dòng nào phải đứng trước? Vì sao?
  builder.Services.AddSingleton<GreetingService>(); phải đứng trước var app = builder.Build();.
builder.Services là danh sách các dịch vụ đang chờ đăng ký. Khi gọi Build(), ASP.NET Core dựng DI container từ danh sách này và khóa nó lại.
Nếu đăng ký sau Build(), chương trình sẽ báo lỗi InvalidOperationException (“The service collection cannot be modified because it is read-only”). Dù không báo lỗi thì container cũng đã tạo xong, nên GreetingService không có trong đó. Lúc đó endpoint POST /greetings không lấy được GreetingService service qua DI.
Thứ tự này khớp với 2 pha ghi trong code: pha 1 là cấu hình và đăng ký dịch vụ, pha 2 là Build(), ánh xạ endpoint rồi Run().


Câu 2	Trong /hello/{name}?language=en, tham số name lấy từ đâu, language lấy từ đâu?
name lấy từ route, tức phần đường dẫn. Tham số có tên trùng với placeholder {name} trong mẫu "/hello/{name}", nên với /hello/Hieu thì name = "Hieu".
language lấy từ query string, tức phần sau dấu ?. Kiểu string? đơn giản không có trong mẫu route, nên Minimal API tự lấy giá trị từ query. Với ?language=en thì language = "en". Nếu không truyền thì language = null vì kiểu là nullable, và API trả lời bằng tiếng Việt (“Xin chào, …”).

Câu 3 Vì sao POST hợp lệ trả Results.Created (201) chứ không phải Results.Ok (200)?
Theo chuẩn HTTP, 201 Created nghĩa là request đã tạo ra một tài nguyên mới. Đó đúng là việc của POST /greetings: tạo một StoredGreeting mới có Id riêng. Còn 200 OK chỉ nói “thành công” chung chung, không cho biết có tài nguyên mới hay không.
Results.Created($"/greetings/{greeting.Id}", greeting) còn gắn thêm header Location: /greetings/1, chỉ ra địa chỉ của tài nguyên vừa tạo. Client nhờ đó biết ngay URI để truy cập sau này, và body vẫn chứa đối tượng vừa tạo.
Dùng đúng mã trạng thái giúp API rõ nghĩa và đúng quy ước REST. Client, công cụ test và tài liệu OpenAPI đều phân biệt được “đọc thành công” (200) với “tạo mới thành công” (201).



 
## 4. MiniDistributedSystem (Bước 9)
- Ảnh chụp 2 terminal API + Client
  <img width="1920" height="1080" alt="Screenshot (7)" src="https://github.com/user-attachments/assets/564b3127-352f-494d-a658-8fab6d00b9cc" />
 <img width="1920" height="1080" alt="Screenshot (8)" src="https://github.com/user-attachments/assets/e5c5bbf8-94a1-4da9-b416-ebbfc9bd1846" />

- Trả lời 3 câu hỏi
 1. Client gọi những endpoint nào? Mỗi cặp request–response ra sao?

Client gọi 2 endpoint, mỗi lần gọi là một cặp yêu cầu–phản hồi (MEP request–response):

#	Method	URL	Body gửi đi	Mã trạng thái (bình thường)	Body nhận về
1	GET	http://localhost:5000/api/assignments	không có	200 OK	Mảng JSON Assignment (2 bài tập mẫu)
2	POST	http://localhost:5000/api/submissions	JSON CreateSubmissionRequest (assignmentId=1, studentCode="SV001", repositoryUrl=...)	201 Created, kèm header Location: /api/submissions/{guid}	JSON SubmissionResult (id, status="received", ...)
Nếu dữ liệu POST không hợp lệ (thiếu StudentCode/RepositoryUrl, hoặc AssignmentId <= 0), API trả 400 Bad Request. Khi đó response.EnsureSuccessStatusCode() ném ra HttpRequestException.
API còn có GET /, trả 200 để kiểm tra dịch vụ đang chạy, nhưng client không gọi endpoint này. Chỉ file requests.http dùng nó.

2. Client có dùng chung code của API không? Hai bên thống nhất bằng gì?

Không. Submission.Client.csproj không có <ProjectReference> tới Submission.Api. Client tự khai báo lại các record Assignment, CreateSubmissionRequest, SubmissionResult trong code của mình.

Hai bên chỉ thống nhất với nhau qua hợp đồng giao tiếp (API contract), gồm:

Giao thức HTTP: method, đường dẫn URL, mã trạng thái.
Định dạng dữ liệu JSON: hai bên dùng cùng tên trường và cùng kiểu dữ liệu (“hình dạng” JSON giống nhau). API tuần tự hóa record thành JSON, client giải tuần tự JSON đó vào record của chính nó.

Nhờ vậy hai tiến trình độc lập với nhau. Client có thể viết bằng ngôn ngữ khác (Python, JS...) mà vẫn gọi được, miễn là tuân theo đúng hợp đồng. Đổi lại, nếu một bên đổi tên trường mà bên kia không đổi theo thì sẽ hỏng. Lỗi này không bị phát hiện lúc biên dịch, chỉ lộ ra khi chạy.

3. Vì sao địa chỉ API truyền qua tham số -- http://localhost:5000 mà không viết cứng?

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
Trong hệ phân tán, client và server là hai tiến trình riêng, có thể chạy trên máy, cổng hoặc môi trường khác nhau (dev, test, production, máy khác trong mạng LAN...). Địa chỉ API là cấu hình triển khai, không phải logic chương trình.
Truyền qua tham số thì đổi server mà không phải sửa code hay biên dịch lại. Ví dụ: dotnet run --project src/Submission.Client -- http://192.168.1.10:8080.
Dấu -- dùng để tách tham số của dotnet run với tham số truyền vào chương trình. http://localhost:5000 đi vào args[0].
Code vẫn có giá trị mặc định http://localhost:5000 để tiện chạy thử trên máy. Nhưng giá trị này khớp với cổng mà API được chạy bằng --urls http://localhost:5000, tức là cả hai bên đều nhận cấu hình từ bên ngoài, không gắn chặt vào một môi trường.
## 5. Thí nghiệm lỗi (Bước 10)
| # | Dự đoán | Thực tế | Đúng/Sai | Giải thích |
  a | Thêm bài tập 3 vào API, client **không cần sửa code** vẫn in ra 3 bài. | Client in `- 1: Bai tap 1`, `- 2: Bai tap 2`, `- 3: Bai tap 3`, nộp bài vẫn `Trang thai: received`. | Đúng | Client không viết cứng dữ liệu mà lấy danh sách qua `GET /api/assignments` rồi duyệt `foreach`. Dữ liệu thay đổi nhưng **hợp đồng** (URL + dạng JSON `{id, title, description}`) không đổi nên client không phải sửa. |
| b | Tắt API rồi chạy client sẽ báo lỗi không kết nối được. | `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5000)` (SocketException 10061). | Đúng | Không có tiến trình nào lắng nghe ở cổng 5000 nên hệ điều hành từ chối kết nối TCP. Gọi qua mạng phụ thuộc vào một tiến trình khác, vào mạng, vào cổng, và các thứ này đều có thể hỏng độc lập với client. Gọi hàm trong cùng chương trình thì code nằm cùng tiến trình, cùng bộ nhớ, nên luôn gọi được. Vì vậy trong hệ phân tán phải tính đến trường hợp lỗi khi gọi qua mạng. |
| c | Gửi `StudentCode: ""` thì API trả 400, client dừng ở `EnsureSuccessStatusCode()`. | Client vẫn in được danh sách bài tập, sau đó báo `HttpRequestException: Response status code does not indicate success: 400 (Bad Request)` tại dòng `EnsureSuccessStatusCode()`. Body API trả về: `{"message":"Du lieu nop bai khong hop le"}`. | Đúng | `EnsureSuccessStatusCode()` kiểm tra mã trạng thái, nếu không phải 2xx thì ném exception, để client không đọc nhầm body lỗi như dữ liệu thành công. Máy chủ phải tự kiểm tra dữ liệu vì không thể tin client: client có thể bị lỗi, viết bằng ngôn ngữ khác, hoặc bị người dùng gửi thẳng request bằng `curl`/REST Client mà bỏ qua mọi kiểm tra phía client. |
| d | API chạy cổng 5050 còn client gọi 5000 thì bị từ chối kết nối giống thí nghiệm b. | Client gọi 5000: `No connection could be made because the target machine actively refused it. (localhost:5000)`. Chạy lại client với `-- http://localhost:5050`: in đủ 2 bài tập và `Trang thai: received`. | Đúng | Sửa **không cần đổi code**, chỉ cần truyền đúng địa chỉ qua tham số dòng lệnh: `dotnet run --project src/Submission.Client -- http://localhost:5050`. Client đọc địa chỉ từ `args[0]`, nên địa chỉ API là cấu hình bên ngoài, không gắn cứng trong code. |
## 6. Dự án EduSubmit tự làm (Bước 11-12)
### Ảnh chụp kết quả

**2 terminal: API (trái) và Client (phải)**

<img width="1920" height="1080" alt="Screenshot (9)" src="https://github.com/user-attachments/assets/c07736e9-60f8-4d9a-ada6-2b7327f4c39d" />

```
Danh sach bai tap:
- 1: Bai tap 1
- 2: Bai tap 2
Da nop bai. Ma luot nop: ac8db7b4-e663-46f9-aa01-40be8cbf81fb
Trang thai: received
```

**Kiểm thử bằng REST Client (`requests.http`): 200, 200, 201, 400**

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/a5f28bff-da12-465e-b42f-bf7b40088723" />
<img width="1920" height="1080" alt="Screenshot (14)" src="https://github.com/user-attachments/assets/a15e38d6-0ef5-4b2e-a730-32acbb9ea183" />
<img width="1920" height="1080" alt="Screenshot (15)" src="https://github.com/user-attachments/assets/dce5ee3a-c554-4b73-b86e-8f230e667c07" />
<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/3bca1704-6ba5-4c4e-9efe-b272414221ae" />

### Phần nào phải xem code mẫu?

Phần 11.2 (Submission.Api) và 11.3 (Submission.Client) được trợ lý AI hướng dẫn viết code, có đối chiếu với code mẫu MiniDistributedSystem, chủ yếu là cách kiểm tra dữ liệu trong `MapPost`, cách dùng `Results.Created(...)`, và các hàm `GetFromJsonAsync` / `PostAsJsonAsync` / `ReadFromJsonAsync`. Phần tạo solution (11.1) tự làm theo hướng dẫn.

### Luồng thông điệp

```
Client --(HTTP GET  /api/assignments)--> Api : request-response (200 + mảng bài tập)
Client --(HTTP POST /api/submissions)--> Api : request-response (201 Created + Location + lượt nộp; dữ liệu sai thì 400)
```xxxxxx


 
## 7. Khó khăn gặp phải và cách giải quyết
