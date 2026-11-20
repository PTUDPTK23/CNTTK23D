using Microsoft.AspNetCore.SignalR.Client;

var baseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{baseUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

await connection.StartAsync();
Console.WriteLine("Đã kết nối SignalR.");
var reply = await connection.InvokeAsync<string>("Echo", "Xin chào SignalR");
Console.WriteLine(reply);
