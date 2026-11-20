using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = "http://localhost:5200";
var name = "Client";

// Đọc tham số dòng lệnh linh hoạt
if (args.Length == 1)
{
    if (args[0].StartsWith("http", StringComparison.OrdinalIgnoreCase))
        serverUrl = args[0].TrimEnd('/');
    else
        name = args[0];
}
else if (args.Length >= 2)
{
    serverUrl = args[0].TrimEnd('/');
    name = args[1];
}
else
{
    Console.Write("Nhap ten client (Enter de dung 'Client'): ");
    var input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input))
        name = input.Trim();
}

// ① Xây dựng kết nối tới hub
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// ② Đăng ký hàm xử lý cho từng TÊN thông điệp
connection.On<string>("EchoResponse",
    msg => Console.WriteLine($"[Echo] {msg}"));

connection.On<string>("BroadcastMessage",
    m => Console.WriteLine($"[Broadcast] {m}"));

connection.On<string>("GroupMessage",
    m => Console.WriteLine($"[Group] {m}"));

connection.On<string>("SystemMessage",
    m => Console.WriteLine($"[System] {m}"));

connection.Reconnecting += _ => {
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += newId => {
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {newId?[..8]}");
    return Task.CompletedTask;
};

connection.Closed += _ => {
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

// ③ Mở kết nối: negotiate + chọn WebSocket
await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi.");

// Gọi các phương thức trên hub
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();
