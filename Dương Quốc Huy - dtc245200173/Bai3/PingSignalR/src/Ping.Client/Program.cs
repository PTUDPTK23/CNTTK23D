using Microsoft.AspNetCore.SignalR.Client;

const string groupName = "lop-hoc-01";
var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("EchoResponse", message => Console.WriteLine($"[Echo] {message}"));
connection.On<string>("BroadcastMessage", message => Console.WriteLine($"[Broadcast] {message}"));
connection.On<string>("GroupMessage", message => Console.WriteLine($"[Group] {message}"));
connection.On<string>("SystemMessage", message => Console.WriteLine($"[System] {message}"));

connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += async id =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {id}");
    try
    {
        await connection.InvokeAsync("JoinGroup", groupName);
    }
    catch (Exception error)
    {
        Console.Error.WriteLine($"[Lifecycle] Khong the vao lai nhom: {error.Message}");
    }
};

connection.Closed += error =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    if (error is not null)
        Console.Error.WriteLine(error.Message);
    return Task.CompletedTask;
};

try
{
    await connection.StartAsync();
    Console.WriteLine($"{name} da ket noi den PingApi. ConnectionId: {connection.ConnectionId}");
    await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");
    await connection.InvokeAsync("JoinGroup", groupName);
    await connection.InvokeAsync("SendToGroup", groupName, $"Thong bao nhom tu {name}");
    await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");

    Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
    Console.ReadLine();
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"{error.GetType().Name}: {error.Message}");
    return 1;
}
