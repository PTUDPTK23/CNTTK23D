var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "Ping.Api", status = "running" }));
app.MapHub<PingHub>("/hubs/ping");
app.Run();

sealed class PingHub : Microsoft.AspNetCore.SignalR.Hub
{
    public string Echo(string message) => $"Echo: {message}";
}
