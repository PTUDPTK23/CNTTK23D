# Bài 2 — RESTful API

## Ảnh kết quả thực hành

### Bước 2 — Chạy Catalog API và kiểm tra endpoint `/`

**Ảnh 1.** API trả về `200 OK` và thông tin trạng thái dưới dạng JSON.

![Bước 2 - Kiểm tra endpoint gốc của Catalog API](buoc-2-api-200.png)

### Bước 3 — Kiểm tra danh sách sản phẩm

**Ảnh 1.** Gọi `GET /api/products` trả về `200 OK` và danh sách 3 sản phẩm.

![Bước 3 - Kiểm tra danh sách sản phẩm](buoc-3-danh-sach-san-pham.png)

### EduSubmit — Chạy API và kiểm tra bước 4

**Ảnh 1.** Submission API đang chạy tại `http://localhost:5000`.

![EduSubmit - Chạy API trên cổng 5000](edusubmit-api-cong-5000.png)

**Ảnh 2.** Kiểm tra bài tập có ID `1`, ID `999` không tồn tại và ID `abc` không hợp lệ. Các kết quả lần lượt là `200 OK`, `404` kèm `ASSIGNMENT_NOT_FOUND` và `404` với body rỗng.

![EduSubmit - Kiểm tra chi tiết bài tập](edusubmit-buoc-4.png)

### EduSubmit — Bước 5: Nộp bài

**Ảnh 1.** Nộp bài hợp lệ trả `201 Created` và `Location`. Dữ liệu rỗng trả `400 INVALID_SUBMISSION`, còn bài tập không tồn tại trả `404 ASSIGNMENT_NOT_FOUND`.

![EduSubmit - Kiểm tra nộp bài](edusubmit-buoc-5.png)
