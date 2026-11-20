
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Dang ky SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Kiem tra Server
app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }));

// Dang ky Hub
app.MapHub<PingHub>("/hubs/ping");

app.Run();

// Hub xu ly Echo, Broadcast va Group
sealed class PingHub : Hub
{
    // Echo rieng cho Client goi
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    // Gui tin cho tat ca Client
    public async Task Broadcast(string sender, string message)
    {
        await Clients.All.SendAsync("BroadcastMessage", $"{sender}: {message}");
    }

    // Cho Client tham gia Group
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("GroupStatus", $"Da tham gia group: {groupName}");
    }

    // Gui tin cho cac Client trong Group
    public async Task SendGroup(string groupName, string sender, string message)
    {
        await Clients.Group(groupName).SendAsync(
            "GroupMessage", groupName, $"{sender}: {message}");
    }

    // Roi Group
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("GroupStatus", $"Da roi group: {groupName}");
    }
}
