
using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/')
                                : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";

// Xay dung ket noi SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// Nhan Echo
connection.On<string>("EchoResponse",
    msg => Console.WriteLine($"[Echo] {msg}"));
// Nhan Broadcast
connection.On<string>("BroadcastMessage",
    m => Console.WriteLine($"[Broadcast] {m}"));

// Nhan Group
connection.On<string>("GroupMessage",
    m => Console.WriteLine($"[Group] {m}"));

// Nhan thong bao he thong
connection.On<string>("SystemMessage",
    m => Console.WriteLine($"[System] {m}"));

// Dang thu ket noi lai
connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

// Ket noi lai thanh cong
connection.Reconnected += newId =>
{
    Console.WriteLine(
        $"[Lifecycle] Da ket noi lai: {newId?[..8]}");
    return Task.CompletedTask;
};

// Ket noi da dong
connection.Closed += _ =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

// Mo ket noi
await connection.StartAsync();

Console.WriteLine($"{name} da ket noi den PingApi.");

// Goi Echo
await connection.InvokeAsync(
    "Echo", $"Xin chao SignalR tu {name}");

// Tham gia nhom
await connection.InvokeAsync(
    "JoinGroup", "lop-hoc-01");

// Gui thong diep nhom
await connection.InvokeAsync(
    "SendToGroup",
    "lop-hoc-01", $"Thong bao nhom tu {name}");

// Gui Broadcast
await connection.InvokeAsync(
    "Broadcast", $"Xin chao tat ca tu {name}");

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();
