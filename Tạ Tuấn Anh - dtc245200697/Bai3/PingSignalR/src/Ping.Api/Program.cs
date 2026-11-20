using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();
app.MapHub<PingHub>("/hubs/ping");

app.MapGet("/", () => "Hello World!");

app.Run();
sealed class PingHub : Hub
{
    public async Task Echo(string message)
 {
 await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
 }

}