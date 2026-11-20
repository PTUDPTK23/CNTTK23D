using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();
app.MapHub<PingHub>("/hubs/ping");

app.MapGet("/", () => "Hello World!");

app.Run();
sealed class PingHub : Hub { }