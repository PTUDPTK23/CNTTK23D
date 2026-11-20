using Microsoft.AspNetCore.SignalR.Client;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var serverUrl = args.FirstOrDefault(value => !value.StartsWith("--", StringComparison.Ordinal))
    ?? "http://localhost:5200";
var interactive = args.Contains("--interactive", StringComparer.OrdinalIgnoreCase);
string? activeGroup = null;

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl.TrimEnd('/')}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

var echoReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
connection.On<string>("EchoResponse", message =>
{
    Console.WriteLine($"[Echo] {message}");
    echoReceived.TrySetResult(true);
});
connection.On<string>("BroadcastMessage", message => Console.WriteLine($"[Broadcast] {message}"));
connection.On<string>("SystemMessage", message => Console.WriteLine($"[System] {message}"));
connection.On<PingGroupMessage>("GroupMessage", message =>
    Console.WriteLine($"[Group {message.Group}] {message.Sender}: {message.Message}"));

connection.Reconnecting += error =>
{
    Console.WriteLine($"Dang ket noi lai... {error?.Message}");
    return Task.CompletedTask;
};

connection.Reconnected += async _ =>
{
    Console.WriteLine("Da ket noi lai.");
    if (!string.IsNullOrWhiteSpace(activeGroup))
        await connection.InvokeAsync("JoinGroup", activeGroup);
};

try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");
    await connection.InvokeAsync("Echo", "Xin chao SignalR");

    if (args.Contains("--auto-exit", StringComparer.OrdinalIgnoreCase))
    {
        await Task.WhenAny(echoReceived.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        return 0;
    }

    if (!interactive)
    {
        Console.WriteLine("Nhan Enter de dong may khach.");
        Console.ReadLine();
        return 0;
    }

    Console.WriteLine("Lenh: echo <noi dung> | broadcast <noi dung> | join <nhom>");
    Console.WriteLine("      group <nhom> <noi dung> | quit");
    while (true)
    {
        Console.Write("> ");
        var line = Console.ReadLine();
        if (line is null || line.Equals("quit", StringComparison.OrdinalIgnoreCase))
            break;

        var separator = line.IndexOf(' ');
        var command = separator < 0 ? line : line[..separator];
        var arguments = separator < 0 ? "" : line[(separator + 1)..].Trim();
        switch (command.ToLowerInvariant())
        {
            case "echo" when arguments.Length > 0:
                await connection.InvokeAsync("Echo", arguments);
                break;
            case "broadcast" when arguments.Length > 0:
                await connection.InvokeAsync("Broadcast", arguments);
                break;
            case "join" when arguments.Length > 0:
                activeGroup = arguments;
                await connection.InvokeAsync("JoinGroup", activeGroup);
                break;
            case "group" when arguments.Contains(' '):
                var groupSeparator = arguments.IndexOf(' ');
                await connection.InvokeAsync(
                    "SendToGroup",
                    arguments[..groupSeparator],
                    arguments[(groupSeparator + 1)..]);
                break;
            default:
                Console.WriteLine("Lenh khong hop le. Nhap lai theo mau tren.");
                break;
        }
    }

    await connection.StopAsync();
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Loi ket noi SignalR: {exception.Message}");
    return 1;
}

record PingGroupMessage(string Group, string Sender, string Message);
