
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
sealed class PingHub : Hub
{
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    public Task Broadcast(string message)
        => Clients.All.SendAsync("BroadcastMessage", message);

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    public Task SendToGroup(string groupName, string message)
        => Clients.Group(groupName).SendAsync("GroupMessage", message);
}
