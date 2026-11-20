
using Microsoft.AspNetCore.SignalR.Client;

var hubUrl = args.Length > 0 ? args[0] : "http://localhost:5000/hubs/ping";

Console.WriteLine("=== PingSignalR Client ===");
Console.WriteLine($"Hub: {hubUrl}");

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("Pong", message =>
{
    Console.WriteLine($"[Pong] {message}");
});

connection.Reconnecting += error =>
{
    Console.WriteLine($"Dang ket noi lai: {error?.Message}");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.WriteLine($"Da ket noi lai. ConnectionId: {connectionId}");
    return Task.CompletedTask;
};

connection.Closed += error =>
{
    Console.WriteLine($"Ket noi da dong: {error?.Message}");
    return Task.CompletedTask;
};

try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    await connection.InvokeAsync("Ping", "Hello SignalR");
    Console.WriteLine("Nhan Enter de thoat.");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine($"Khong the ket noi: {ex.Message}");
}
finally
{
    await connection.DisposeAsync();
}
