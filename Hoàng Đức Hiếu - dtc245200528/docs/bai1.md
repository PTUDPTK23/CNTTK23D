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
Dùng đúng mã trạng thái giúp API rõ nghĩa và đúng quy ước REST. Client, công cụ test và tài liệu OpenAPI đều phân biệt được “đọc thành công” (200) với “tạo mới thành công” (201)



 
## 4. MiniDistributedSystem (Bước 9)
- Ảnh chụp 2 terminal API + Client
- Trả lời 3 câu hỏi
 
## 5. Thí nghiệm lỗi (Bước 10)
| # | Dự đoán | Thực tế | Đúng/Sai | Giải thích |
 
## 6. Dự án EduSubmit tự làm (Bước 11-12)
- Ảnh chụp kết quả; phần nào phải xem code mẫu?
- Luồng thông điệp:
  Client --(HTTP GET /api/assignments)--> Api   : request-response
  Client --(HTTP POST /api/submissions)--> Api  : ...
 
## 7. Khó khăn gặp phải và cách giải quyết
