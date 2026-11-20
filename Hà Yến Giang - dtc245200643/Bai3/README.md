\# Bài 3 – Giao tiếp thời gian thực với SignalR



\- \*\*Họ tên:\*\* Hà Yến Giang

\- \*\*MSSV:\*\* DTC245200643

\- \*\*Lớp:\*\* CNTT K23D





\# Trả lời 3 câu hỏi đầu giờ



1\. Máy chủ có thể chủ động báo cho máy khách không?

Có. SignalR duy trì kết nối lâu dài, cho phép Server chủ động gửi thông báo đến Client mà không cần Client hỏi lại.

2\. REST và SignalR chia việc thế nào?

\- REST: Tạo và đọc dữ liệu, là nguồn sự thật.

\- SignalR: Đẩy thông báo thay đổi trạng thái theo thời gian thực.

3\. Đứt mạng đúng lúc máy chủ báo graded thì sao?

Client có thể bị mất thông báo. Khi kết nối lại, Client phải tham gia lại nhóm bằng WatchSubmission(id) và gọi REST GET để lấy trạng thái mới nhất.



\## 1. PingSignalR



\## Bước 1

!\[Build succeeded, 0 Error(s) ](anh/buoc1-build.png)

\## Bước 2

!\[curl negotiate trả connectionId ](buoc2-negotiate.png)

\## Bước 3 + 4

!\[Máy chủ chạy, máy khách in \[Echo] Echo: Xin chao SignalR ](anh/buoc4-echo.png)

\## Bước 5

!\[Hai máy khách thấy tin của nhau, trừ tin Echo ](anh/buoc5-nhom.png)

\## Bước 6

!\[Đang ket noi lai... rồi Da ket noi lai ](anh/buoc6-noilai.png)

\## Bước 7

\## Thí nghiệm 1: Server chưa khởi động

!\[Server chưa chạy](anh/buoc7-loi1.png)

\- Thực hiện: Chạy Client khi Server chưa được khởi động

\- Kết quả: Client báo lỗi Connection refused tại localhost:5200

\- Giải thích: Không có Server lắng nghe tại cổng 5200 nên Client không thể kết nối

\## Thí nghiệm 2: Sai đường dẫn Hub

!\[Sai đường dẫn Hub](anh/buoc7-loi2.png)

\- Thực hiện: Sửa đường dẫn /hubs/ping thành /hub/ping

\- Kết quả: Client nhận lỗi HTTP 404 Not Found.

\- Giải thích: Đường dẫn Client không khớp với endpoint Hub được khai báo trên Server

\## Thí nghiệm 3: Sai tên thông điệp

!\[Sai tên thông điệp](anh/buoc7-loi3.png)

\- Thực hiện: Sửa EchoResponse thành EchoRespone ở Client

\- Kết quả: Client kết nối thành công nhưng không xuất hiện dòng \[Echo]. Các thông điệp \[Group] và \[Broadcast] vẫn hoạt động

\- Giải thích: Tên sự kiện Server gửi và Client đăng ký nhận không trùng nhau nên thông điệp Echo bị bỏ qua. Khi không nhận được dữ liệu, cần kiểm tra tên sự kiện ở cả hai phía



\# 2 – EduSubmit nhận thông báo trạng thái thời gian thực (RealtimeSubmissionSystem)

Ý chính: giữ nguyên REST của Bài 2 để tạo và đọc lượt nộp; thêm hub `/hubs/submissions` để máy chủ \*\*đẩy\*\* trạng thái `processing`, `graded` tới đúng nhóm `submission:{id}`. Máy chủ cổng \*\*5000\*\*.

Thiết kế kênh thời gian thực (khung 4 câu hỏi, trả lời trước khi code):

| Câu hỏi | Trả lời | Code tương ứng |

|---|---|---|

| ① Sự kiện nào kích hoạt thông điệp? | Mỗi lần trạng thái lượt nộp đổi: received → processing → graded | `UpdateStatusAsync(id, status, ...)` |

| ② Gửi cho ai? | Chỉ máy khách đang theo dõi đúng lượt nộp đó, không dùng `Clients.All` | `Clients.Group("submission:{id}")` |

| ③ Gửi kèm dữ liệu gì? | Mã lượt nộp, trạng thái mới, thời điểm đổi | `record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt)` |

| ④ Mất kết nối thì lấy lại trạng thái thế nào? | SignalR không lưu tin; gọi lại REST | `GET /api/submissions/{id}` |



\## EduSubmit – Bước 1: tạo solution RealtimeSubmissionSystem

!\[build](anh/edusubmit-buoc1-build.png)



Thấy: Restore complete, hai project Submission.RealtimeClient và Submission.RealtimeApi đều hiển thị succeeded, cuối cùng xuất hiện Build succeeded in 8.0s, không có lỗi biên dịch.



\## EduSubmit – Bước 2: REST của Bài 2 + UpdatedAt + khóa dữ liệu

!\[post](anh/edusubmit-buoc2.png)

Thấy: `201 Created`, header `Location: /api/submissions/{id}`, body có `"status":"received"` và `"updatedAt"`.

Giải thích: `UpdatedAt` là thời điểm đổi trạng thái gần nhất, máy khách so sánh để biết tin nào mới hơn. `lock` vì ở bước 4 tác vụ chấm nền và các yêu cầu HTTP cùng sửa danh sách; chỉ một luồng được đọc/ghi tại một thời điểm. REST là \*\*nguồn sự thật\*\* (câu ④).



\## EduSubmit – Bước 3: hub mỏng, đưa kết nối vào nhóm của lượt nộp

!\[negotiate](anh/edusubmit-buoc3-negotiate.png)



Thấy: negotiate ở /hubs/submissions trả connectionId và 3 cách truyền tải /hub/submissions trả 404 Not Found

Giải thích: hub mỏng – không tạo, không chấm bài, nghiệp vụ nằm ở REST và tác vụ nền. Dùng nhóm theo lượt nộp vì 2 lý do: riêng tư sinh viên không thấy trạng thái bài của người khác và tiết kiệm băng thông chỉ gửi đúng người, thay vì Clients.All gửi cho tất cả rồi máy khách tự lọc nhóm do máy chủ quản lý. 



\## EduSubmit – Bước 4: IHubContext và mô phỏng chấm bài ở nền

Terminal 1 – máy chủ in khi đẩy thông báo:

!\[may chu](anh/edusubmit-buoc4-may-chu.png)



Terminal 2 – POST rồi GET 3 lần (chưa cần máy khách):

!\[rest](anhchupminhchung/edusubmit-buoc4-rest.png)



Thấy: POST `/api/assignments/1/submissions` trả `201 Created` ngay với `status` là `received`; ba lần GET theo id (ngay lập tức, sau \~1,5 giây, sau \~4 giây) cho đúng thứ tự `received` → `processing` → `graded`, `updatedAt` tăng dần (+1 giây, rồi +3 giây so với `receivedAt`).



Giải thích: POST không chờ chấm bài – tác vụ nền được khởi động bằng `\_ = SimulateGradingAsync(...)` (không `await`) nên người nộp nhận 201 ngay, còn chấm bài tiếp tục phía sau. Tác vụ nền không dùng được đối tượng hub vì hub chỉ sống trong một lời gọi, nên dùng `IHubContext<SubmissionHub>` (ASP.NET Core tự đưa vào qua DI) để gửi từ ngoài hub. Mỗi lần đổi trạng thái làm theo thứ tự: ① cập nhật dữ liệu → ② tạo tin tối thiểu `SubmissionStatusMessage` (chỉ 3 trường, không gửi cả `SubmissionDto`) → ③ đẩy đến đúng nhóm `submission:{id}`. Lưu trước, đẩy sau, nên ai đọc REST ngay khi nhận tin cũng thấy đúng trạng thái. REST luôn biết trạng thái đúng; SignalR chỉ giúp máy khách biết sớm hơn, không phải hỏi lại. Hệ thật sẽ thay bằng hàng đợi + worker (Bài 6), cách đẩy về giữ nguyên.



\## EduSubmit – Bước 5: máy khách mở hai kênh và gắn chuông chờ graded



!\[ket noi](anh/edusubmit-buoc5-ket-noi.png)



Thấy: chạy máy khách khi máy chủ đang chạy, in `Da ket noi SignalR.`.

Giải thích: máy khách dùng hai kênh trên cùng một URL gốc – REST qua `HttpClient` (`BaseAddress`) và SignalR qua `HubConnection` (`/hubs/submissions`, có `WithAutomaticReconnect`). `On<SubmissionStatusMessage>("SubmissionStatusChanged", ...)` đăng ký trước `StartAsync`, SignalR tự chuyển JSON thành record nên record phải khai báo giống máy chủ (cùng tên trường `SubmissionId`, `Status`, `UpdatedAt`). `TaskCompletionSource` là "cái chuông": một Task hoàn tất khi gọi `TrySetResult`, dùng để chờ sự kiện – ở đây là thông báo `graded`.



\## EduSubmit – Bước 6: tạo bằng REST, theo dõi bằng SignalR, chốt lại bằng REST



Terminal 1 – máy chủ (trái), Terminal 2 – máy khách SV001 (phải):



!\[graded](anh/edusubmit-buoc6-graded.png)



Kết quả máy khách (8 dòng):

Da ket noi SignalR.

Chon bai tap: 1 - Bai tap 1

Da tao luot nop: 77d4d72e-50f4-4aa0-bab2-a9a56b82e600

Trang thai REST ban dau: received

Dang cho thong bao trang thai tu may chu...

Thong bao SignalR: 77d4d72e-50f4-4aa0-bab2-a9a56b82e600 -> processing luc 11:49:31

Thong bao SignalR: 77d4d72e-50f4-4aa0-bab2-a9a56b82e600 -> graded luc 11:49:33

Trang thai doc lai bang REST: graded



Thấy: máy khách in đủ 8 dòng – REST (`Chon bai tap`, `Da tao luot nop`, `Trang thai REST ban dau: received`), SignalR (`Thong bao SignalR ... -> processing` lúc 11:49:31 và `... -> graded` lúc 11:49:33, cách nhau khoảng 2 giây), rồi REST đọc lại (`Trang thai doc lai bang REST: graded`). Máy chủ in `\[Hub] ... theo doi submission:{id}`, `\[Push] ... -> processing`, `\[Push] ... -> graded`.



Giải thích: hai cập nhật `processing` và `graded` đến từ máy chủ mà máy khách không hề hỏi. Thứ tự đúng: ① REST lấy danh sách bài tập → ② REST tạo lượt nộp (kiểm tra mã 201 trước khi đọc body, lỗi thì in `ApiError`) → ③ `WatchSubmission(id)` để vào nhóm `submission:{id}` (chỉ gọi được khi đã có id từ POST) → ④ `Task.WhenAny` chờ chuông `graded` hay đồng hồ 10 giây, cái nào xong trước → ⑤ GET đọc lại. Trạng thái cuối luôn lấy từ REST (quy tắc vàng): REST luôn biết trạng thái đúng, SignalR chỉ giúp máy khách biết sớm hơn, không phải hỏi lại, và nếu lỡ mất thông báo (hết 10 giây) thì lần đọc lại bằng REST vẫn cho kết quả đúng.



\## EduSubmit – Bước 7: năm thí nghiệm phân tán



Dự đoán (ghi TRƯỚC khi chạy):

| # | Thí nghiệm | Dự đoán | Kết quả thật | Bài học |

|---|---|---|---|---|

| 1 | Chạy 2 máy khách SV001 và SV002 cùng lúc | Mỗi máy khách chỉ nhận thông báo của lượt nộp mình vừa tạo, không thấy của người kia | Mỗi máy khách chỉ in thông báo của lượt nộp mình vừa tạo (SV001: `afc22a5c`, SV002: `cf76991b`) | Nhóm `submission:{id}` tách người nhận |

| 2 | Chạy máy khách khi máy chủ chưa chạy | `HttpRequestException` không kết nối được, dừng ngay ở `StartAsync` | | |

| 3 | Sửa `WithUrl` thành `/hub/submissions` | Lỗi `404 (Not Found)` ở bước negotiate | | |

| 4 | Sửa `On("SubmissionStatusChanged")` thành `On("SubmissionStatusChange")` | Không có lỗi, không có dòng "Thong bao SignalR"; sau 10 giây in "Qua thoi gian cho thong bao graded."; REST vẫn đọc ra `graded` | | |

| 5 | Chờ 4 giây rồi mới gọi `WatchSubmission` | Không nhận thông báo nào (đã chấm xong trước khi vào nhóm); hết 10 giây; REST đọc ra `graded` | | |



Kết quả chạy thật:



Thí nghiệm 1 – hai máy khách SV001 (trái) và SV002 (phải) cùng lúc:



!\[tn1](anh/edusubmit-tn1-hai-may-khach.png)



Thí nghiệm 2 – máy chủ chưa chạy:



!\[tn2](anh/edusubmit-tn2-may-chu-tat.png)



Thí nghiệm 3 – sai đường dẫn `/hub/submissions`:



!\[tn3](anh/edusubmit-tn3-sai-hub.png)



Thí nghiệm 4 – sai tên sự kiện `SubmissionStatusChange`:



!\[tn4](anh/edusubmit-tn4-sai-ten.png)



Thí nghiệm 5 – chờ 4 giây rồi mới `WatchSubmission` (máy khách, rồi máy chủ):



!\[tn5](anh/edusubmit-tn5-watch-muon.png)

| # | Dự đoán | Kết quả thật | Đúng/Sai | Giải thích |

| 1 | Mỗi máy khách chỉ thấy lượt nộp của mình | SV001 chỉ nhận thông báo của `931300b7`, SV002 chỉ nhận của `ae5217e5`; máy chủ in 2 dòng `\[Hub] ... theo doi` với 2 nhóm khác nhau | Đúng | Nhóm `submission:{id}` tách người nhận: máy chủ chỉ đẩy tới kết nối đã `WatchSubmission` đúng id. Riêng tư và không tốn băng thông như `Clients.All`. |

| 2 | `HttpRequestException` không kết nối được | `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5026)` (`SocketException 10061`), dừng ở `StartAsync` → `NegotiateAsync`, `Program.cs` dòng 28; không in `Da ket noi SignalR.` | Đúng | `StartAsync` gọi negotiate tới cổng 5026 nhưng không có tiến trình nào nghe. Lần nối \*\*đầu tiên\*\* không được tự thử lại; `WithAutomaticReconnect` chỉ cứu kết nối đã mở. |

| 3 | 404 ở bước negotiate | `HttpRequestException: Response status code does not indicate success: 404 (Not Found)` tại `HttpConnection.NegotiateAsync` | Đúng | Máy chủ chỉ `MapHub` ở `/hubs/submissions`; `/hub/submissions/negotiate` không có tuyến. Đường dẫn `WithUrl` phải khớp `MapHub`. |

| 4 | Không lỗi, hết 10 giây, REST ra graded | Không có lỗi, \*\*không có dòng "Thong bao SignalR"\*\*; in `Qua thoi gian cho thong bao graded.`; `Trang thai doc lai bang REST: graded` | Đúng | Máy chủ vẫn gửi `"SubmissionStatusChanged"` nhưng máy khách nghe `"SubmissionStatusChange"`; SignalR bỏ qua thông điệp không ai đăng ký, không báo lỗi. Sai tên chiều xuống = \*\*im lặng\*\*. Nhờ bước GET đọc lại nên vẫn có trạng thái đúng. |

| 5 | Không nhận tin nào, REST ra graded | Không có dòng "Thong bao SignalR", hết 10 giây; REST đọc ra `graded`. Máy chủ in `\[Push] processing`, `\[Push] graded` \*\*trước\*\* `\[Hub] ... theo doi` | Đúng | Chấm xong sau 3 giây nhưng máy khách vào nhóm sau 4 giây: tin đã gửi cho nhóm lúc chưa có ai → \*\*lỡ tin\*\*, SignalR không lưu để gửi lại. \*\*REST là nguồn sự thật\*\*: luôn đọc lại bằng GET khi kết thúc hoặc khi nối lại. |



\## EduSubmit – Bước 8: requests.http gom các ca REST và bước bắt tay của hub

!\[requests](anh/edusubmit-buoc8-requests.png)

Kết quả: 10/10 ca kiểm thử đạt yêu cầu. REST API và SignalR Hub hoạt động đúng, các trường hợp lỗi trả về đúng HTTP Status Code.

\---





