using Microsoft.AspNetCore.SignalR;

public sealed class PingHub : Hub
{
    public Task Ping(string message)
    {
        return Clients.Caller.SendAsync("Pong", $"Server received: {message}");
    }
}
