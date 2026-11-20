# Bài 1 - Ngô Thị Như Quỳnh

MSSV: `dtc245200620`

## Chạy chương trình

```powershell
dotnet build EduSubmit.slnx

# Terminal 1
dotnet run --project src/Submission.Api --urls http://localhost:5000

# Terminal 2
dotnet run --project src/Submission.Client -- http://localhost:5000
```

File kiểm thử REST Client: `requests.http`.

Báo cáo và ảnh minh chứng: `docs/bai1.md`.
