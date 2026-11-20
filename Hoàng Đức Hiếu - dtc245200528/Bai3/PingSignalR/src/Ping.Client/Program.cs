using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/')
                                : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";


await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();


connection.On<string>("EchoResponse",
    msg => Console.WriteLine($"[Echo] {msg}"));


await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi.");
