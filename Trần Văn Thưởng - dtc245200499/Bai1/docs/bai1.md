# Bài 1 – Tổng quan hệ thống và MEP (Đề 20)

Họ tên: **Trần Văn Thưởng** MSSV: **dtc245200499** Lớp: **CNTT K23D**

## 1. Cài đặt

SDK .NET: **[ghi kết quả `dotnet --version`]**. Git: **[ghi kết quả `git --version`]**.

Ảnh chụp màn hình bước 4: **[thêm ảnh thật vào docs và chèn liên kết tại đây]**.

## 2. MepDemo (Bước 7)

- MEP 1 – request-response: client gửi yêu cầu và chờ phản hồi tương ứng.
- MEP 2 – one-way: bên gửi phát thông điệp mà không chờ phản hồi nghiệp vụ.
- MEP 3 – queue-based: thông điệp vào hàng đợi và mỗi thông điệp được một worker nhận xử lý; nhiều worker chia nhau công việc.
- MEP 4 – publish-subscribe: một sự kiện được phát tới tất cả subscriber đã đăng ký.
- MEP 5 – streaming: dữ liệu được truyền liên tục theo luồng thay vì chờ một phản hồi đơn lẻ.
- MEP 3: mỗi sub-x do **một** worker xử lý.
- MEP 4: mỗi sự kiện được gửi tới **cả 3 dịch vụ**.

## 3. BasicWebApi (Bước 8)

- `AddSingleton` phải được gọi trước `builder.Build()` vì cấu hình dịch vụ hoàn tất trước khi ứng dụng được xây dựng.
- `name` lấy từ route `/hello/{name}`; `language` lấy từ query string `?language=en`.
- `201 Created` thể hiện tài nguyên mới đã được tạo và thường kèm `Location`; `200 OK` chỉ cho biết yêu cầu thành công.
- Bài tự làm: cần thêm `language=ja` trả `Konnichiwa, {name}!` và chụp kết quả chạy thật: **[bổ sung]**.
- Ảnh REST Client cho 200/201/400: **[bổ sung ảnh thật]**.

## 4. MiniDistributedSystem (Bước 9)

- Client gọi `GET /api/assignments` (200) để lấy danh sách và `POST /api/submissions` (201 khi hợp lệ) để nộp bài; nếu dữ liệu không hợp lệ API trả 400.
- Hai project không dùng chung code API. Chúng thống nhất qua hợp đồng HTTP/JSON: đường dẫn, method, tên trường và kiểu dữ liệu.
- URL API nhận qua tham số để có thể đổi môi trường/host/cổng mà không sửa rồi biên dịch lại mã nguồn.
- Ảnh hai terminal chạy thật: **[bổ sung ảnh thật]**.

## 5. Thí nghiệm lỗi (Bước 10)

Điền cột thực tế sau khi tự chạy từng thao tác. Dự đoán bên dưới là dự đoán ban đầu để đối chiếu.

| # | Dự đoán | Thực tế (điền sau khi chạy) | Đúng/Sai | Giải thích |
|---|---|---|---|---|
| a | Client vẫn thấy thêm bài tập vì tải danh sách từ API ở mỗi lần chạy. | [ghi kết quả] | [ ] | Dữ liệu và logic danh mục thuộc API; client không mã hóa danh sách vào chương trình. |
| b | Client báo `HttpRequestException`/connection refused khi API tắt. | [ghi thông báo thật] | [ ] | Gọi qua mạng phụ thuộc tiến trình máy chủ và kết nối; lời gọi hàm trong cùng tiến trình không cần kết nối mạng. |
| c | API trả 400; `EnsureSuccessStatusCode()` ném `HttpRequestException`. | [ghi mã/thông báo thật] | [ ] | Client có thể gửi dữ liệu sai; máy chủ phải xác thực ở ranh giới tin cậy trước khi xử lý. |
| d | Client vẫn gọi cổng 5000 nên không kết nối được cho tới khi sửa cấu hình chạy. | [ghi cách khắc phục] | [ ] | Truyền URL cổng 5050 cho client bằng tham số dòng lệnh, không sửa code. |

## 6. Dự án Đề 20 – LibraryBooking (Bước 11–12)

- API: `GET /`, `GET /api/books`, `GET /api/books/{id}`, `POST /api/reservations`, `GET /api/reservations/{id}`.
- Client gọi API qua HTTP; không có tham chiếu project giữa client và server.
- Kết quả chạy API + client: **[bổ sung ảnh thật]**.
- Ảnh kiểm thử REST Client 200/201/400/404/409: **[bổ sung ảnh thật]**.
- Phần phải xem code mẫu khi tự gõ từ đầu: **[ghi đúng quá trình thực tế; không áp dụng nếu tự viết toàn bộ]**.

## 7. Khó khăn gặp phải và cách giải quyết

**[ghi vấn đề thực tế và cách khắc phục]**

## 8. Câu hỏi tự kiểm tra

1. Ứng dụng nguyên khối thường triển khai các chức năng trong một tiến trình/đơn vị lớn. Ứng dụng phân tán chia thành tiến trình hoặc dịch vụ độc lập trao đổi qua mạng. Ranh giới trong bài này nằm giữa `Library.Client` và `Library.Api`, qua HTTP.
2. Năm MEP: request-response, one-way, queue-based, publish-subscribe, streaming. Đề 20 dùng request-response.
3. Queue phân phối mỗi thông điệp cho một consumer xử lý, ví dụ hàng đợi đơn hàng. Publish-subscribe phát một sự kiện tới mọi subscriber, ví dụ thông báo thay đổi lịch cho nhiều dịch vụ.
4. 200: thành công; 201: tạo tài nguyên; 400: yêu cầu không hợp lệ; 404: không tìm thấy tài nguyên.
5. Client kiểm tra status trước khi đọc nội dung thành công để không diễn giải lỗi (400/404/500) như dữ liệu hợp lệ và xử lý lỗi đúng cách.

## 9. Nộp bài

- [ ] Bổ sung họ tên, MSSV, lớp và ảnh chụp kết quả chạy thật.
- [ ] Chạy build và thử các request; ghi kết quả thí nghiệm thực tế.
- [ ] Tạo repository Git cá nhân, commit, đẩy code và gắn tag `bai1` theo yêu cầu giảng viên.

