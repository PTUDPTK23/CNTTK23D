# Bài 2 – Catalog API (Phần 2)

## Chạy bài

Trong thư mục catalogapi, chạy:

```powershell
dotnet build CatalogApi.slnx
dotnet run --project src/Catalog.Api --no-launch-profile --urls http://localhost:5100
```

Mở terminal khác để chạy client:

```powershell
dotnet run --project src/Catalog.Client -- http://localhost:5100
```

Có 8 request để thử trong `requests.http`.

## Bước 9

Lúc thử mình dùng cổng 5101 vì 5100 đang bận.

| Thử | Kết quả |
| --- | --- |
| Chạy client lần 2 | Thấy 4 sản phẩm, thêm được sản phẩm số 5. |
| Tắt API rồi chạy client | Kết nối cổng 5101 bị từ chối. |
| Mở lại API | Còn 3 sản phẩm mẫu. |
| Chạy client ở cổng 5999 | Kết nối bị từ chối. |

1. Gọi HTTP có thể lỗi nếu server tắt hoặc sai cổng.
2. Danh sách lưu trong RAM của server nên tắt API thì mất dữ liệu đã thêm.
3. Nếu đổi DTO bên client thành `Ten`, JSON vẫn gửi `name` nên `Ten` sẽ là `null`.

## Bước 10

| # | Kiểm tra | Kết quả |
| --- | --- | --- |
| 1 | GET / | 200 |
| 2 | GET danh sách | 200, 3 sản phẩm lúc mới chạy |
| 3 | GET sản phẩm 1 | 200 |
| 4 | GET sản phẩm 999 | 404, `PRODUCT_NOT_FOUND` |
| 5 | GET sản phẩm abc | 404, body rỗng |
| 6 | POST hợp lệ | 201, Location `/api/products/4` |
| 7 | POST giá âm | 400, `INVALID_PRODUCT` |
| 8 | POST thiếu Content-Type | 415 |
| 9 | Chạy client | Xem danh sách, thêm mới, xem chi tiết, thử lỗi 400 |
| 10 | Chạy client khi API tắt | Lỗi kết nối |

Build không lỗi, đạt 10/10 mục.
