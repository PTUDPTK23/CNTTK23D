# Bai 2 - RESTful Catalog API

**Sinh viên:** Đồng Anh Tuấn (`dtc245200427`)
**Thư mục dự án:** `CatalogApi/`

## Cấu trúc

- `CatalogApi.slnx`: solution gồm `Catalog.Api` và `Catalog.Client`.
- `CatalogApi/src/Catalog.Api/`: API máy chủ, chạy cổng 5100.
- `CatalogApi/src/Catalog.Client/`: client console gọi API qua HTTP/JSON.
- `CatalogApi/requests.http`: 7 yêu cầu mẫu để xem các phản hồi HTTP.
- `CatalogApi/sp-hop-le.json`, `CatalogApi/sp-sai.json`: body mẫu cho POST.

## Chạy

Mở terminal tại `Bai2/CatalogApi`.

```powershell
dotnet build CatalogApi.slnx --no-restore
dotnet run --project src/Catalog.Api --urls http://localhost:5100
```

Mở terminal thứ hai tại cùng thư mục để chạy client:

```powershell
dotnet run --project src/Catalog.Client -- http://localhost:5100
```

Nếu đổi cổng API, truyền URL mới sau dấu `--` cho client. Nếu cổng 5100 đang được dùng, dừng server cũ bằng `Ctrl+C` trước khi chạy lại.

## Nhật ký kiểm tra

Build solution: thành công, 0 cảnh báo, 0 lỗi. Khi kiểm tra, server cũ đang giữ file đầu ra nên bản mới được build sang thư mục tạm; sau khi dừng server có thể build lại vào thư mục mặc định.

| Yêu cầu | Kết quả quan sát |
| --- | --- |
| `GET /` | 200, `service=Catalog.Api`, `status=running` |
| `GET /api/products` | 200, 3 sản phẩm ban đầu |
| `GET /api/products/1` | 200, sản phẩm có Id 1 |
| `GET /api/products/999` | 404, `PRODUCT_NOT_FOUND` |
| `GET /api/products/abc` | 404 do route `{id:int}` không khớp; body rỗng |
| `POST /api/products` với JSON hợp lệ | 201, có `Location: /api/products/4` trên server mới khởi động |
| `POST /api/products` với tên rỗng hoặc giá âm | 400, `INVALID_PRODUCT` |
| `POST /api/products` thiếu `Content-Type` | 415 do framework từ chối body |
| `POST /api/products/1` | 405, `Allow: GET` |

### Thử nghiệm hai tiến trình

- Gọi client nhiều lần khi API còn chạy: sản phẩm mới tiếp tục được thêm vào danh sách trong RAM của server.
- Dừng rồi khởi động lại API: danh sách quay về 3 sản phẩm mẫu, vì dữ liệu chỉ được lưu trong RAM.
- Trỏ client tới `http://localhost:5999`: client in lỗi không kết nối được ra terminal và thoát mã 1; không còn lỗi ngoại lệ chưa xử lý.

## Ba câu hỏi thiết kế

1. **Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?** URI định danh tài nguyên như `/api/products`; phương thức `GET`, `POST`, `PUT`, `DELETE` cho biết thao tác. Nhờ vậy địa chỉ ổn định và cùng một tài nguyên có thể hỗ trợ nhiều thao tác rõ ràng.
2. **“Stateless” thể hiện ở đâu trong code?** Mỗi yêu cầu HTTP tự mang đủ thông tin để xử lý; server không giữ phiên hội thoại riêng cho từng client. Danh sách sản phẩm vẫn là trạng thái của tài nguyên, nhưng chỉ nằm trong RAM server và mất khi tiến trình dừng.
3. **Vì sao client và server khai báo DTO riêng mà vẫn khớp nhau?** Hai chương trình độc lập chỉ cần tuân theo cùng hợp đồng JSON (tên trường và kiểu dữ liệu). Mỗi bên có thể thay đổi mô hình nội bộ mà không cần dùng chung assembly hay mã nguồn.

## Nộp bài

Tiêu đề PR theo hướng dẫn: `Bai2 - dtc245200427 - Đồng Anh Tuấn`.
