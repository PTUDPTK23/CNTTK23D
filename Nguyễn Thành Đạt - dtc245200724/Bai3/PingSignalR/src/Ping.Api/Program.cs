using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();
app.MapGet("/", () => Results.Ok(new { service = "PingApi", status = "running" }));
app.MapHub<PingHub>("/hubs/ping");
app.Run();

sealed class PingHub : Hub
{
    public Task Echo(string message) =>
        Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");

    public Task Broadcast(string message) =>
        Clients.All.SendAsync("BroadcastMessage", message);

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("GroupMessage",
            $"{ShortId} da tham gia nhom {groupName}");
    }

    public Task SendToGroup(string groupName, string message) =>
        Clients.Group(groupName).SendAsync("GroupMessage", message);

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] + {ShortId} ket noi");
        await Clients.All.SendAsync("SystemMessage", $"Client {ShortId} da ket noi.");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[Hub] - {ShortId} ngat");
        await Clients.All.SendAsync("SystemMessage", $"Client {ShortId} da ngat ket noi.");
        await base.OnDisconnectedAsync(exception);
    }

    private string ShortId => Context.ConnectionId[..Math.Min(8, Context.ConnectionId.Length)];
}
