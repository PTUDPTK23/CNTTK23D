using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
var positional = args.Where(arg => arg is not ("--wrong-hub" or "--typo-handler")).ToArray();
var first = positional.FirstOrDefault();
var hasUrl = Uri.TryCreate(first, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
var name = (hasUrl ? positional.ElementAtOrDefault(1) : first) ?? "Nguyễn Việt Đô";
var baseUrl = (hasUrl ? first : Environment.GetEnvironmentVariable("PING_BASE_URL")) ?? "http://localhost:5200";
var url = baseUrl.TrimEnd('/') + (args.Contains("--wrong-hub") ? "/hub/ping" : "/hubs/ping");
const string groupName = "lop-hoc-01";

await using var connection = new HubConnectionBuilder().WithUrl(url).WithAutomaticReconnect().Build();
string? lastId = null;
var wantsGroup = false;
var echoEvent = args.Contains("--typo-handler") ? "EchoRespone" : "EchoResponse";
connection.On<string>(echoEvent, message => Console.WriteLine($"[EchoResponse] {message}"));
connection.On<string>("BroadcastMessage", message => Console.WriteLine($"[BroadcastMessage] {message}"));
connection.On<string>("GroupMessage", message => Console.WriteLine($"[GroupMessage] {message}"));
connection.On<string>("SystemMessage", message => Console.WriteLine($"[SystemMessage] {message}"));

connection.Reconnecting += error =>
{
    Console.WriteLine($"[Reconnecting] ID cũ: {lastId}. {error?.Message}");
    return Task.CompletedTask;
};
connection.Reconnected += async id =>
{
    lastId = id ?? connection.ConnectionId;
    Console.WriteLine($"[Reconnected] ID mới: {lastId}");
    if (Volatile.Read(ref wantsGroup))
    {
        try
        {
            await connection.InvokeAsync("JoinGroup", groupName);
            Console.WriteLine($"Đã vào lại nhóm {groupName}");
        }
        catch (Exception error)
        {
            Console.WriteLine($"Không vào lại được nhóm {groupName}: {error.Message}");
        }
    }
};
connection.Closed += error =>
{
    Console.WriteLine($"[Closed] ID cuối: {lastId}. {error?.Message}");
    return Task.CompletedTask;
};

Console.WriteLine($"Đang kết nối: {url}");
try
{
    await connection.StartAsync();
}
catch (HttpRequestException error)
{
    var reason = error switch
    {
        { StatusCode: { } status } => $"HTTP {(int)status}: {error.Message}",
        { InnerException: SocketException { SocketErrorCode: SocketError.ConnectionRefused } }
            => $"Connection refused: {error.Message}",
        _ => error.Message
    };
    Console.Error.WriteLine($"[Kết nối thất bại] {reason}");
    return 1;
}
lastId = connection.ConnectionId;
Console.WriteLine($"Đã kết nối: {lastId}");
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");
Console.WriteLine("[Echo] Máy chủ đã xử lý lời gọi.");
Console.WriteLine($"Lệnh: broadcast <tin nhắn> | join (vào {groupName}) | group <tin nhắn> | Enter để thoát.");
while (Console.ReadLine() is { } line && !string.IsNullOrWhiteSpace(line))
{
    var parts = line.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (connection.State != HubConnectionState.Connected)
    {
        Console.WriteLine("Chưa có kết nối. Hãy chờ kết nối lại hoặc nhấn Enter để thoát.");
        continue;
    }
    try
    {
        switch (parts)
        {
            case ["broadcast", var message]:
                await connection.InvokeAsync("Broadcast", $"{name}: {message}");
                break;
            case ["join"]:
                Volatile.Write(ref wantsGroup, true);
                await connection.InvokeAsync("JoinGroup", groupName);
                Console.WriteLine($"Đã vào nhóm {groupName}");
                break;
            case ["group", var message]:
                await connection.InvokeAsync("SendToGroup", groupName, $"{name}: {message}");
                break;
            default:
                Console.WriteLine("Lệnh không hợp lệ.");
                break;
        }
    }
    catch (Exception error)
    {
        Console.WriteLine($"Không thực hiện được lệnh: {error.Message}");
    }
}
return 0;
