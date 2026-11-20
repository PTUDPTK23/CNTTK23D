using Microsoft.AspNetCore.SignalR;
var builder = WebApplication.CreateBuilder(args);
// Việc 1: đăng ký dịch vụ SignalR vào DI
builder.Services.AddSignalR();
var app = builder.Build();
app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }));
// Việc 2: ánh xạ hub vào tuyến /hubs/ping
app.MapHub<PingHub>("/hubs/ping");
app.Run();
sealed class PingHub : Hub
{
    public Task Echo(string message) => Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");

    public Task Broadcast(string message) => Clients.All.SendAsync("BroadcastMessage", message);

    public Task JoinGroup(string groupName) => Groups.AddToGroupAsync(Context.ConnectionId, groupName);

    public Task SendToGroup(string groupName, string message) =>
        Clients.Group(groupName).SendAsync("GroupMessage", $"[{groupName}] {message}");

    public override Task OnConnectedAsync() =>
        Clients.All.SendAsync("SystemMessage", $"Đã kết nối: {Context.ConnectionId}");

    public override Task OnDisconnectedAsync(Exception? exception) =>
        Clients.All.SendAsync("SystemMessage", $"Đã ngắt kết nối: {Context.ConnectionId}");
}