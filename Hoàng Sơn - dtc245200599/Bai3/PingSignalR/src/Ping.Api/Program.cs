
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Việc 1: Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }));

// Việc 2: Ánh xạ Hub vào tuyến /hubs/ping
app.MapHub<PingHub>("/hubs/ping");

app.Run();

// Tạo Hub SignalR
sealed class PingHub : Hub { }
