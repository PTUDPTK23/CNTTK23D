# Dự án Tuần 1 – Đề tài 04: Hệ thống đặt món và xử lý tại bếp

- **Môn học:** Phát triển ứng dụng phân tán
- **Họ tên:** Nguyễn Đức Mạnh
- **MSSV:** dtc245200152
- **Môi trường:** .NET SDK 10 (`net10.0`)

Giai đoạn hiện tại: **Bài 1–2** (Minimal API + RESTful API + client console). Quy tắc dự án: [QUY_TAC_DU_AN.md](QUY_TAC_DU_AN.md).

## Thành phần

- `src/Order.Api`: Web API, lưu dữ liệu trong bộ nhớ.
- `src/Order.Client`: ứng dụng console gọi API qua HTTP.

## Cách chạy

```
dotnet build OrderSystem.slnx
```

Terminal 1:

```
dotnet run --project src/Order.Api --urls http://localhost:5000
```

Terminal 2:

```
dotnet run --project src/Order.Client -- http://localhost:5000
```

## Endpoint

| Method | URI | Kết quả |
| --- | --- | --- |
| GET | `/` | 200, API đang chạy |
| GET | `/menu` | 200, danh sách món |
| POST | `/orders` | 201 + Location, hoặc 400 `ApiError` |
| GET | `/orders/{id}` | 200, hoặc 404 `ORDER_NOT_FOUND` |
| GET | `/orders?customer=...&status=...` | 200, hoặc 400 `INVALID_STATUS` |
