using Microsoft.AspNetCore.SignalR;
var builder = WebApplication.CreateBuilder(args);
// Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();
var app = builder.Build();
app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }
));
// Ánh xạ Hub
app.MapHub<PingHub>("/hubs/ping");
app.Run();
sealed class PingHub : Hub
{
    // Echo: phản hồi riêng người gọi
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync(
            "EchoResponse", $"Echo: {message}");
    }
    // Broadcast: gửi đến tất cả Client
    public async Task Broadcast(string message)
        => await Clients.All.SendAsync("BroadcastMessage", message);

    // JoinGroup: tham gia nhóm
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }
    // SendToGroup: gửi tin nhắn đến nhóm
    public async Task SendToGroup(string groupName, string message)
        => await Clients.Group(groupName).SendAsync("GroupMessage", message);
    // Theo dõi khi Client kết nối
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(
            $"[Hub] + {Context.ConnectionId[..8]} ket noi");

        await Clients.All.SendAsync(
            "SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ket noi.");

        await base.OnConnectedAsync();
    }
    // Theo dõi khi Client ngắt kết nối
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
