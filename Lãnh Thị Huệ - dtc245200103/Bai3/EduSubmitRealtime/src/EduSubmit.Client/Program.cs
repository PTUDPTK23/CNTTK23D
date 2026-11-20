using EduSubmit.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5201";
var classId = args.Length > 1 ? args[1] : "CNTT-K21";

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("JoinedClass", msg =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[He thong] {msg}");
    Console.ResetColor();
});

connection.On<SubmissionNotification>("NewSubmission", item =>
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n>>> CO BAI NOP MOI [{item.SubmittedAt:HH:mm:ss}] <<<");
    Console.WriteLine($"    Ma bai   : {item.SubmissionId}");
    Console.WriteLine($"    Sinh vien: {item.StudentName} ({item.StudentId})");
    Console.WriteLine($"    Lop      : {item.ClassId}");
    Console.WriteLine($"    Bai tap  : {item.AssignmentTitle}");
    Console.WriteLine($"    Tep tin  : {item.FileName}");
    Console.WriteLine($"    Trang thai: {item.Status}\n");
    Console.ResetColor();
});

await connection.StartAsync();
Console.WriteLine($"Da ket noi toi EduSubmit Server ({serverUrl}).");

await connection.InvokeAsync("JoinClass", classId);

Console.WriteLine("Dang lang nghe bai nop theo thoi gian thuc... Nhan Enter de thoat.");
Console.ReadLine();