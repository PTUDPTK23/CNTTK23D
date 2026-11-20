using LibrarySystem.Stores;
using LibrarySystem.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký Singleton - dùng chung cho mọi request
builder.Services.AddSingleton<BookStore>();

var app = builder.Build();

// Map endpoints
app.MapBookEndpoints();

// Health check
app.MapGet("/", () => new
{
    message = "Hệ thống Thư viện - Library System API",
    version = "1.0.0",
    endpoints = new[]
    {
        "GET /books              - Tra cứu danh sách sách",
        "GET /books/{id}         - Tra cứu chi tiết sách",
        "GET /books/{id}/availability - Tra cứu số bản khả dụng"
    }
});

app.Run();
