using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status}");
    if (message.Status == "graded")
        gradedNotification.TrySetResult();
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
