using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";

// Xay dung ket noi SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// Dang ky Echo
connection.On<string>("EchoResponse",
    msg => Console.WriteLine($"[Echo] {msg}"));

// Dang ky Broadcast voi ten su kien dung
connection.On<string>("BroadcastMessage",
    msg => Console.WriteLine($"[Broadcast] {msg}"));

// Dang ky Group
connection.On<string, string>("GroupMessage",
    (group, msg) => Console.WriteLine($"[Group - {group}] {msg}"));

// Dang ky thong bao Group
connection.On<string>("GroupStatus",
    msg => Console.WriteLine($"[Group] {msg}"));

// Thong bao khi dang ket noi lai
connection.Reconnecting += error =>
{
    Console.WriteLine("Dang ket noi lai...");
    return Task.CompletedTask;
};

// Thong bao khi ket noi lai thanh cong
connection.Reconnected += connectionId =>
{
    Console.WriteLine("Da ket noi lai");
    return Task.CompletedTask;
};

// Thong bao khi ket noi dong
connection.Closed += error =>
{
    Console.WriteLine("Ket noi da dong");
    return Task.CompletedTask;
};

// Ket noi Server
await connection.StartAsync();

Console.WriteLine($"{name} da ket noi den PingApi.");
Console.WriteLine("Cac lenh:");
Console.WriteLine("echo <tin>");
Console.WriteLine("broadcast <tin>");
Console.WriteLine("join <ten-nhom>");
Console.WriteLine("group <ten-nhom> <tin>");
Console.WriteLine("leave <ten-nhom>");
Console.WriteLine("exit");

// Vong lap nhap lenh
while (true)
{
    Console.Write($"{name}> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

    try
    {
        switch (parts[0].ToLower())
        {
            case "echo" when parts.Length >= 2:
                await connection.InvokeAsync("Echo",
                    parts[1] + (parts.Length == 3 ? " " + parts[2] : ""));
                break;

            case "broadcast" when parts.Length >= 2:
                await connection.InvokeAsync("Broadcast", name,
                    input.Substring("broadcast ".Length));
                break;

            case "join" when parts.Length >= 2:
                await connection.InvokeAsync("JoinGroup", parts[1]);
                break;

            case "group" when parts.Length == 3:
                await connection.InvokeAsync("SendGroup", parts[1], name, parts[2]);
                break;

            case "leave" when parts.Length >= 2:
                await connection.InvokeAsync("LeaveGroup", parts[1]);
                break;

            default:
                Console.WriteLine("Lenh khong hop le. Hay nhap theo danh sach lenh.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Loi: {ex.Message}");
    }
}