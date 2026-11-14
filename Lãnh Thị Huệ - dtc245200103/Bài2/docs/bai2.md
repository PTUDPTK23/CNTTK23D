# Bài 2 – Lập trình API với .NET (CatalogApi)
Họ tên: Lãnh Thị Huệ          MSSV: dtc245200103          Lớp: CNTTK23D

## 1. Cấu hình máy chủ và DTO (Bước 1 - Bước 2)
- Khởi tạo solution `CatalogApi` gồm 2 dự án: `Catalog.Api` (máy chủ) và `Catalog.Client` (máy khách).
- Cấu hình cổng chạy mặc định tại `http://localhost:5100` trong `launchSettings.json`.
- Khai báo các kiểu dữ liệu truyền tải (DTO): `ProductDto`, `CreateProductRequest`, `ApiError` và khởi tạo danh sách 3 sản phẩm mẫu ban đầu trong bộ nhớ RAM.

## 2. Kiểm thử Endpoint bằng curl.exe (Bước 3 - Bước 5)
- `GET /`: Trả về `200 OK` xác nhận máy chủ Kestrel đang hoạt động.
- `GET /api/products`: Trả về `200 OK` cùng danh sách 3 sản phẩm định dạng JSON.
- `GET /api/products/1`: Trả về `200 OK` chi tiết sản phẩm có ID = 1.
- `GET /api/products/999`: Trả về `404 Not Found` kèm thông báo lỗi chuẩn hóa `ApiError` (`PRODUCT_NOT_FOUND`).
- `GET /api/products/abc`: Trả về `404 Not Found` rỗng do ràng buộc kiểu tuyến đường `{id:int}`.
- `POST /api/products` (dữ liệu hợp lệ từ `sp-hop-le.json`): Trả về `201 Created`, header `Location: /api/products/4` và dữ liệu sản phẩm mới tạo.
- `POST /api/products` (dữ liệu sai giá âm từ `sp-sai.json`): Trả về `400 Bad Request` kèm mã lỗi `INVALID_PRODUCT`.
- `POST /api/products` (thiếu header `Content-Type`): Trả về `415 Unsupported Media Type`.

## 3. Kiểm thử bằng REST Client - requests.http (Bước 6)
- Tạo tệp `requests.http` lưu trữ đầy đủ 8 kịch bản kiểm thử (`200`, `201`, `400`, `404`, `415`).
- Các yêu cầu chạy thành công trực tiếp trên VS Code và trả về đúng mã trạng thái HTTP như thiết kế.
*(Chèn ảnh chụp màn hình VS Code chạy file requests.http và cửa sổ Response tại đây)*

## 4. Ứng dụng máy khách Catalog.Client (Bước 7 - Bước 8)
- Xây dựng chương trình console sử dụng `HttpClient` nhận địa chỉ máy chủ qua tham số dòng lệnh (`http://localhost:5100`).
- Thực hiện tuần tự 4 nấc: lấy danh sách sản phẩm -> gửi `POST` tạo mới sản phẩm `[4] Nhap mon REST API` và đọc header `Location` -> đọc chi tiết sản phẩm `[1]` -> gửi dữ liệu sai để bắt lỗi `400 BadRequest` an toàn.
*(Chèn ảnh chụp 2 cửa sổ Terminal: máy chủ Catalog.Api đang chạy và kết quả in ra của Catalog.Client tại đây)*

## 5. Thí nghiệm phân tán (Bước 9)
| # | Thí nghiệm | Dự đoán | Thực tế | Đúng/Sai | Giải thích |
|---|---|---|---|---|---|
| 1 | Chạy máy khách lần 2 khi máy chủ vẫn bật | Danh sách có 4 sản phẩm, sản phẩm mới mang ID 5 | Danh sách hiện 4 sản phẩm (có cả ID 4 vừa tạo), sản phẩm mới nhận ID 5 (`Location: /api/products/5`) | Đúng | Trạng thái (dữ liệu) được lưu giữ tập trung tại tiến trình máy chủ, không lưu ở máy khách. |
| 2 | Tắt máy chủ (`Ctrl + C`) rồi chạy máy khách | Máy khách báo lỗi không thể kết nối | Văng ngoại lệ `HttpRequestException: No connection could be made... (localhost:5100)` | Đúng | Trong hệ phân tán, tiến trình cung cấp dịch vụ có thể dừng hoặc vắng mặt tại thời điểm gọi. |
| 3 | Khởi động lại máy chủ và kiểm tra danh sách | Dữ liệu mới tạo biến mất, chỉ còn 3 sản phẩm gốc | `GET /api/products` trả về đúng 3 sản phẩm ban đầu, các sản phẩm 4 và 5 không còn | Đúng | Dữ liệu lưu tạm trong bộ nhớ RAM (`List<ProductDto>`) sẽ mất sạch khi tiến trình máy chủ tắt. |
| 4 | Chạy máy khách gọi sang cổng sai (`5999`) | Máy khách bị từ chối kết nối | Báo lỗi `HttpRequestException: No connection could be made... (localhost:5999)` | Đúng | Hai tiến trình phân tán bắt buộc phải cấu hình trùng khớp chính xác địa chỉ mạng (giao thức, host, cổng). |

## 6. Lưu trữ mã nguồn và luồng thông điệp (Bước 10)
- Khởi tạo kho lưu trữ Git (`git init`), cấu hình `.gitignore` loại bỏ thư mục `bin/`, `obj/` và commit toàn bộ 12 tệp mã nguồn.
- Luồng thông điệp chính giữa Client và Server:
  - `Client --(HTTP GET /api/products)--> Api` : Request-Response (`200 OK` + JSON Array)
  - `Client --(HTTP POST /api/products)--> Api` : Request-Response (`201 Created` + Header `Location` + JSON Object / hoặc `400 Bad Request` + `ApiError`)

## 7. Khó khăn gặp phải và cách giải quyết
- **Lỗi lệch thư mục khi chạy `curl.exe`:** Ban đầu tạo tệp `sp-hop-le.json` nhầm thư mục nên báo lỗi không đọc được file; đã kiểm tra lại bằng lệnh `dir` và tạo đúng tệp tại thư mục gốc `CatalogApi`.
- **Lỗi định dạng tệp `requests.http`:** Cần đảm bảo có đúng 1 dòng trống ngăn cách giữa phần header `Content-Type: application/json` và phần thân JSON `{ ... }` để REST Client phân tách đúng gói tin HTTP.