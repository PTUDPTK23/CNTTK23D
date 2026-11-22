# Solution khởi tạo cho 21 đề REST API và SignalR

Hai project sử dụng .NET 10, có thể biên dịch trước khi sinh viên làm bài.

- ExamStarter.Api là ASP.NET Core Minimal API. SignalR server có sẵn trong shared framework ASP.NET Core.
- ExamStarter.Client là Console App, tham chiếu Microsoft.AspNetCore.SignalR.Client phiên bản 10.0.0.
- offline-feed chứa tất cả gói NuGet cần cho SignalR Client và những phụ thuộc trực tiếp, gián tiếp.
- NuGet.Config chỉ trỏ tới nguồn gói cục bộ.

## Điều kiện máy thi

Cài sẵn .NET SDK 10, bao gồm ASP.NET Core Runtime, bằng bộ cài offline trước buổi thi. Sao chép toàn bộ thư mục starter-solution, bao gồm offline-feed.

Trong PowerShell tại thư mục starter-solution, thực hiện:

    dotnet restore ExamStarter.slnx --configfile NuGet.Config -p:NuGetAudit=false
    dotnet build ExamStarter.slnx --no-restore

Chạy hai terminal riêng:

    dotnet run --project ExamStarter.Api --no-restore --urls http://localhost:5000
    dotnet run --project ExamStarter.Client --no-restore

Solution chỉ có mã khởi tạo và tham chiếu cần thiết. Sinh viên tự triển khai endpoints, Hub, các handler nhận sự kiện và HttpClient theo tên đối tượng, API, event trong đề được phát.

Phần A gồm GET danh sách, GET chi tiết, POST và kiểm tra dữ liệu. Phần B gồm SignalR Hub và event sau POST. Phần C gồm Console Client nhận SignalR và gọi REST API. Phần D gồm PUT hoặc DELETE và thông báo SignalR tương ứng.

Khi thu bài, nộp cả solution và mã nguồn. Nếu cần chấm trên máy không có cache NuGet, giữ NuGet.Config và offline-feed. Không cần nộp bin hoặc obj.
