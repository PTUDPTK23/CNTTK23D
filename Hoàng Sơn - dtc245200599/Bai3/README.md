# Bài 3 – SignalR

## Bước 1.1 – Tạo solution và hai project

Tạo solution PingSignalR, project ASP.NET Core Ping.Api và project console Ping.Client.

![Tạo solution PingSignalR](anh/buoc1-tao-solution.png)

![Tạo Ping.Api và Ping.Client](anh/buoc1-tao-project.png)

## Bước 1.2 – Thêm hai project vào solution và build

Cả hai project có trong solution. Máy khách dùng Microsoft.AspNetCore.SignalR.Client 10.0.15. File .gitignore bỏ qua thư mục bin và obj.

    dotnet build

Kết quả kiểm tra: build thành công, 0 cảnh báo, 0 lỗi.

![Kết quả dotnet build](anh/buoc1-build.png)

## Bước 2 – Chạy Ping.Api và kiểm tra negotiate

Chạy máy chủ từ thư mục PingSignalR rồi giữ cửa sổ đó mở.

    dotnet run --project src/Ping.Api --urls http://localhost:5200

Trong cửa sổ terminal thứ hai, kiểm tra ba phản hồi sau.

    curl.exe http://localhost:5200/
    curl.exe -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"
    curl.exe -i http://localhost:5200/hubs/ping

GET / xác nhận PingApi đang chạy. POST negotiate trả connectionId và các kiểu truyền tải WebSockets, ServerSentEvents, LongPolling. GET trực tiếp vào hub thiếu ID kết nối nên trả 400 với thông báo Connection ID required.

![Kết quả POST negotiate có connectionId](anh/buoc2-negotiate.png)

![Máy chủ lắng nghe ở cổng 5200](anh/buoc2-server.png)

![Phản hồi 400 khi gọi hub mà thiếu ID kết nối](anh/buoc2-get-hub-400.png)

## Bước 3 – Thêm phương thức Echo

Máy khách gọi Echo với nội dung tin nhắn. Hub dùng Clients.Caller để gửi sự kiện EchoResponse về đúng kết nối vừa gọi, với nội dung có tiền tố `Echo:`.

    dotnet build
    dotnet run --project src/Ping.Api --urls http://localhost:5200

Sau khi sửa Program.cs, dừng máy chủ cũ bằng Ctrl+C rồi khởi động lại. Bước 4 sẽ dùng máy khách SignalR để gọi Echo và chụp kết quả cho cả hai bước.

## Bước 4.1 – Kết nối máy khách SignalR

Ping.Client mặc định dùng http://localhost:5200 và nhận địa chỉ máy chủ, tên máy khách qua tham số dòng lệnh. Client đăng ký hàm in EchoResponse trước khi mở kết nối tới /hubs/ping và bật tự kết nối lại.

Kết quả kiểm tra: build thành công, 0 cảnh báo, 0 lỗi.

## Bước 4.2 – Gọi Echo và nhận phản hồi

Từ thư mục PingSignalR, chạy máy chủ trong terminal thứ nhất.

    dotnet run --project src/Ping.Api --urls http://localhost:5200

Chạy máy khách trong terminal thứ hai.

    dotnet run --project src/Ping.Client -- http://localhost:5200 An

Máy khách gọi Echo qua InvokeAsync. Khi nhận EchoResponse, nó in phản hồi rồi chờ Enter để thoát.

Kết quả chạy thử trên cổng tạm 49351 vì cổng 5200 đang được máy chủ khác sử dụng.

    An da ket noi den PingApi.
    [Echo] Echo: Xin chao SignalR tu An
    Dang cho thong diep... Nhan Enter de thoat.

## Bước 5 – Phát rộng và gửi tin theo nhóm

### Bước 5.1 – Thêm phương thức cho PingHub

Broadcast dùng Clients.All để gửi cho mọi kết nối đang mở, kể cả người gửi. JoinGroup thêm kết nối hiện tại vào nhóm và thông báo cho nhóm. SendToGroup dùng Clients.Group để gửi tin cho các kết nối thuộc nhóm. Echo vẫn dùng Clients.Caller để trả lời riêng người gọi.

### Bước 5.2 – Đăng ký sự kiện và gọi phương thức từ máy khách

Máy khách đăng ký BroadcastMessage và GroupMessage trước StartAsync. Sau khi kết nối và gọi Echo, máy khách vào nhóm lop-hoc-01, gửi tin nhóm, rồi phát rộng. Build cả hai project thành công với 0 cảnh báo và 0 lỗi.

### Bước 5.3 – Chạy An và Bình, đối chiếu trang 35

Từ thư mục PingSignalR, khởi động lại máy chủ trong terminal thứ nhất.

    dotnet run --project src/Ping.Api --urls http://localhost:5200

Chạy An trong terminal thứ hai và giữ kết nối mở.

    dotnet run --project src/Ping.Client -- http://localhost:5200 An

Sau khi An vào nhóm, chạy Bình trong terminal thứ ba.

    dotnet run --project src/Ping.Client -- http://localhost:5200 Binh

Kết quả chạy thử trên cổng tạm 64414 vì cổng 5200 đang có máy chủ khác chạy. Các dòng chính trong terminal của An gồm:

    [Echo] Echo: Xin chao SignalR tu An
    [Group] Thong bao nhom tu Binh
    [Broadcast] Xin chao tat ca tu Binh

Terminal của Bình in:

    [Echo] Echo: Xin chao SignalR tu Binh
    [Group] Thong bao nhom tu Binh
    [Broadcast] Xin chao tat ca tu Binh

An không nhận Echo của Bình. Bình tham gia sau nên không thấy các tin An đã gửi trước đó. Dòng [System] trong hình minh họa sẽ được thêm ở bước 6.

## Bước 6 – Vòng đời kết nối và tự kết nối lại

PingHub ghi nhật ký [Hub] khi có kết nối mở hoặc đóng và gửi SystemMessage cho các máy khách đang kết nối. Ping.Client đăng ký SystemMessage, Reconnecting, Reconnected và Closed trước StartAsync.

Từ thư mục PingSignalR, chạy máy chủ và máy khách An trong hai terminal.

    dotnet run --project src/Ping.Api --urls http://localhost:5200
    dotnet run --project src/Ping.Client -- http://localhost:5200 An

Giữ An mở, dùng Ctrl+C để dừng máy chủ rồi khởi động lại ở cùng địa chỉ. Với lịch mặc định, máy khách chờ lần lượt 0, 2, 10 và 30 giây trước mỗi lần thử. Nếu cả bốn lần đều thất bại, sự kiện Closed được gọi.

Kiểm tra trang 39 trên cổng tạm 59242 vì cổng 5200 đang có máy chủ khác chạy. Các dòng sau xuất hiện trong terminal của An.

    An da ket noi den PingApi. ConnectionId: LeFcO19M
    [Lifecycle] Dang ket noi lai...
    [Lifecycle] Da ket noi lai: RV4t3yxt
    [System] Client RV4t3yxt da ket noi.

Sau khi An kết nối lại, Bình vào nhóm và gửi tin. An nhận Broadcast và thông báo Bình ngắt kết nối. Tin Group của Bình chỉ hiện ở Bình vì An đã có ConnectionId mới và chưa gọi JoinGroup lại. Máy chủ in các dòng [Hub] tương ứng, và máy khách in Closed khi kết thúc.

## Bước 7 – Ba thí nghiệm lỗi kết nối

| Ca | Cách thử | Kết quả quan sát | Giải thích |
| --- | --- | --- | --- |
| 1. Máy chủ chưa chạy | Chạy Ping.Client đến cổng 52733 trên máy cục bộ, nơi chưa có Ping.Api lắng nghe. | HttpRequestException, máy đích từ chối kết nối. | StartAsync đang mở kết nối lần đầu. WithAutomaticReconnect chỉ thử nối lại sau khi một kết nối đã mở bị mất. |
| 2. Sai đường dẫn hub | Giữ Ping.Api chạy ở cổng 52740. Trong bản thử của Ping.Client, đổi WithUrl từ /hubs/ping thành /hub/ping. | HttpRequestException: Response status code does not indicate success: 404 (Not Found). | Client gửi negotiate tới đường dẫn chưa được máy chủ đăng ký. WithUrl phải khớp với MapHub. |
| 3. Sai tên thông điệp | Dùng lại /hubs/ping. Trong bản thử, đổi tên đăng ký từ EchoResponse thành EchoRespone. | Client kết nối, vẫn in [Group] và [Broadcast], không có dòng [Echo]. Chương trình thoát với mã 0. | Hub gửi sự kiện EchoResponse. Client đăng ký EchoRespone nên không gọi hàm in Echo. |

Hai biến thể được chạy từ bản sao tạm của máy khách. Chạy đối chứng bằng bản gốc với đường dẫn /hubs/ping và tên EchoResponse cho kết quả:

    [Echo] Echo: Xin chao SignalR tu An

### Ôn tập – Bốn phạm vi gửi ở trang 41

| Phạm vi | Ai nhận | Ví dụ |
| --- | --- | --- |
| Clients.All | Mọi kết nối đang mở, gồm cả người gửi. | Thông báo sắp bảo trì cho mọi máy khách. |
| Clients.Caller | Chỉ kết nối vừa gọi phương thức hub. | Trả Echo về cho người gọi. |
| Clients.Group(tên) | Các kết nối thuộc nhóm chỉ định. | Báo trạng thái mới cho nhóm theo dõi một lượt nộp. |
| Clients.Client(id) | Kết nối có ConnectionId được chỉ định. | Gửi riêng thông báo đến một phiên làm việc. |

## EduSubmit – Bước 1: Tạo solution và hai project

Solution RealtimeSubmissionSystem gồm máy chủ ASP.NET Core Submission.RealtimeApi và máy khách console Submission.RealtimeClient. Máy khách tham chiếu Microsoft.AspNetCore.SignalR.Client 10.0.15. Hai project đã được thêm vào solution và .gitignore loại thư mục bin và obj.

Từ thư mục RealtimeSubmissionSystem, chạy:

    dotnet sln list
    dotnet build --tl:off

Danh sách solution hiện đủ hai project. Kết quả build: 0 cảnh báo, 0 lỗi.

## EduSubmit – Bước 2: API REST và thời điểm cập nhật

Chuyển các tuyến đọc bài tập, tạo và đọc lượt nộp từ Bài 2 sang Submission.RealtimeApi. POST kiểm tra bài tập có tồn tại và còn mở, sau đó kiểm tra mã sinh viên và URL repository. Lượt nộp mới có trạng thái received, với ReceivedAt và UpdatedAt lấy cùng một thời điểm UTC. API dùng chung submissionsLock để khóa thao tác thêm và đọc lượt nộp.

Từ thư mục RealtimeSubmissionSystem, chạy máy chủ:

    dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000

Trong PowerShell 7 ở cửa sổ thứ hai, gửi một lượt nộp mẫu:

    curl.exe -i http://localhost:5000/api/assignments/1/submissions --json '{"studentCode":"dtc245200599","repositoryUrl":"https://github.com/hoangsondu7-ops/CNTTK23D"}'

Kiểm tra thực tế bằng curl trên cổng 5000: POST trả 201 Created, Location và JSON chứa status là received, có updatedAt. GET địa chỉ trong Location trả 200 và cùng dữ liệu. Bài tập không tồn tại hoặc lượt nộp không tồn tại trả 404. Bài tập đã đóng hoặc dữ liệu thiếu trả 400.
