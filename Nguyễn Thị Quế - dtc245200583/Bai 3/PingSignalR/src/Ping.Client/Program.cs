using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5200";

var name = args.Length > 1 ? args[1] : "Client";

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// Lắng nghe phản hồi Echo
connection.On<string>("EchoResponse", msg =>
{
    Console.WriteLine($"[Echo] {msg}");
});

// ① Thêm 2 dòng On này
connection.On<string>("BroadcastMessage", msg =>
{
    Console.WriteLine($"[Broadcast] {msg}");
});

connection.On<string>("GroupMessage", msg =>
{
    Console.WriteLine($"[Group] {msg}");
});
// Nhận thông báo từ Server
connection.On<string>("SystemMessage",
    m => Console.WriteLine($"[System] {m}"));

// Khi bắt đầu thử kết nối lại
connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

// Khi kết nối lại thành công
connection.Reconnected += newId =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {newId?[..8]}");
    return Task.CompletedTask;
};

// Khi kết nối đã đóng
connection.Closed += _ =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};
// Kết nối đến Server
await connection.StartAsync();

Console.WriteLine($"{name} da ket noi den PingApi.");

// Gửi Echo
await connection.InvokeAsync(
    "Echo",
    $"Xin chao SignalR tu {name}"
);

// ② Thêm 3 lời gọi Invoke này
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");

await connection.InvokeAsync(
    "SendToGroup",
    "lop-hoc-01",
    $"{name} thong bao nhom"
);

await connection.InvokeAsync(
    "Broadcast",
    $"{name} xin chao tat ca"
);

Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");

Console.ReadLine();