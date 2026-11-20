using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "Ping.Api", status = "running" }));
app.MapHub<PingHub>("/hubs/ping");

app.Run();

sealed class PingHub : Hub
{
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync("BroadcastMessage", message);
    }

    public async Task JoinGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new HubException("Ten nhom khong duoc de trong.");

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            new PingGroupMessage(groupName, Context.ConnectionId, "Da tham gia nhom."));
    }

    public async Task SendToGroup(string groupName, string message)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new HubException("Ten nhom khong duoc de trong.");

        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            new PingGroupMessage(groupName, Context.ConnectionId, message));
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId} connected");
        await Clients.All.SendAsync("SystemMessage", $"Ket noi moi: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId} disconnected");
        await Clients.All.SendAsync("SystemMessage", $"Da ngat ket noi: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}

record PingGroupMessage(string Group, string Sender, string Message);
