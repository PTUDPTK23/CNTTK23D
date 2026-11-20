using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = "http://localhost:5210";
var hubUrl = $"{serverUrl}/hubs/submissions";

Console.Write("Nhap ID bai nop can theo doi: ");
var submissionId = Console.ReadLine()?.Trim();

if (string.IsNullOrWhiteSpace(submissionId))
{
    Console.WriteLine("ID bai nop khong duoc de trong.");
    return;
}

await using var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged",
    message =>
    {
        Console.WriteLine(
            $"[CAP NHAT] Submission: {message.SubmissionId} | " +
            $"Status: {message.Status} | " +
            $"UpdatedAt: {message.UpdatedAt}");
    });

try
{
    await connection.StartAsync();

    Console.WriteLine("Da ket noi SignalR thanh cong!");

    await connection.InvokeAsync("WatchSubmission", submissionId);

    Console.WriteLine($"Dang theo doi bai nop: {submissionId}");
    Console.WriteLine("Nhan Ctrl+C de thoat.");

    await Task.Delay(Timeout.Infinite);
}
catch (Exception ex)
{
    Console.WriteLine($"Loi ket noi: {ex.Message}");
}

public sealed record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);
