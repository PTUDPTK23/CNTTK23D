using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";
const string groupName = "lop-hoc-01";

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("EchoResponse", message => Console.WriteLine($"[Echo] {message}"));
connection.On<string>("BroadcastMessage", message => Console.WriteLine($"[Broadcast] {message}"));
connection.On<string>("GroupMessage", message => Console.WriteLine($"[Group] {message}"));
connection.On<string>("SystemMessage", message => Console.WriteLine($"[System] {message}"));
connection.Reconnecting += _ => { Console.WriteLine("[Lifecycle] Dang ket noi lai..."); return Task.CompletedTask; };
connection.Reconnected += id => { Console.WriteLine($"[Lifecycle] Da ket noi lai: {id?[..Math.Min(8, id.Length)]}"); return Task.CompletedTask; };
connection.Closed += _ => { Console.WriteLine("[Lifecycle] Ket noi da dong."); return Task.CompletedTask; };

await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi. ConnectionId: {connection.ConnectionId?[..8]}");
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");
await connection.InvokeAsync("JoinGroup", groupName);
await connection.InvokeAsync("SendToGroup", groupName, $"Thong bao nhom tu {name}");
await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");
Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();
