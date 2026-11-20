
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Dang ky dich vu SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// API kiem tra may chu
app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }));

// Anh xa Hub SignalR
app.MapHub<PingHub>("/hubs/ping");

app.Run();

// Lop Hub SignalR
sealed class PingHub : Hub
{
    // Echo: tra loi rieng nguoi goi
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync(
            "EchoResponse", $"Echo: {message}");
    }

    // Broadcast: gui den tat ca
    public async Task Broadcast(string message)
        => await Clients.All.SendAsync(
            "BroadcastMessage", message);

    // Tham gia nhom
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId, groupName);

        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    // Gui tin nhan den nhom
    public async Task SendToGroup(
        string groupName, string message)
        => await Clients.Group(groupName).SendAsync(
            "GroupMessage", message);

    // Khi may khach ket noi
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(
            $"[Hub] + {Context.ConnectionId[..8]} ket noi");

        await Clients.All.SendAsync(
            "SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ket noi.");

        await base.OnConnectedAsync();
    }

    // Khi may khach ngat ket noi
    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        Console.WriteLine(
            $"[Hub] - {Context.ConnectionId[..8]} ngat");

        await Clients.All.SendAsync(
            "SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ngat ket noi.");

        await base.OnDisconnectedAsync(ex);
    }
}
