using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

const string BaseUrl = "http://localhost:5000";

// Kết nối đường dây nghe ngóng tới Máy chủ
var connection = new HubConnectionBuilder()
    .WithUrl($"{BaseUrl}/hubs/tasks")
    .Build();

// Khi Máy chủ báo có việc mới -> Tự động in ra màn hình
connection.On<TaskItem>("TaskCreated", task =>
{
    Console.WriteLine($"\n[NEW] {task.Id} - {task.Title}");
    Console.Write("Nhap ten cong viec: ");
});

// Khi Máy chủ báo có việc vừa sửa xong -> Tự động in ra màn hình
connection.On<TaskItem>("TaskUpdated", task =>
{
    var trangThai = task.IsDone ? "Hoan thanh" : "Chua xong";
    Console.WriteLine($"\n[UPDATED] {task.Id} - {trangThai}");
    Console.Write("Nhap ten cong viec: ");
});

// Bắt đầu kết nối
await connection.StartAsync();
Console.WriteLine("Da ket noi thanh cong toi May chu!");

using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

while (true)
{
    Console.Write("Nhap ten cong viec (go 'done 1' de hoan thanh viec so 1, 'exit' de thoat): ");
    var input = Console.ReadLine();

    if (input == "exit") break;

    if (input != null && input.StartsWith("done "))
    {
        if (int.TryParse(input.Substring(5), out int id))
        {
            var putRes = await httpClient.PutAsJsonAsync($"/api/tasks/{id}", new UpdateTaskRequest(true));
            Console.WriteLine(putRes.IsSuccessStatusCode ? "Cap nhat thanh cong!" : "Khong tim thay ID nay!");
        }
        continue;
    }

    var response = await httpClient.PostAsJsonAsync("/api/tasks", new CreateTaskRequest(input ?? ""));

    if (response.IsSuccessStatusCode)
    {
        var created = await response.Content.ReadFromJsonAsync<TaskItem>();
        Console.WriteLine($"-> Da tao thanh cong ID: {created?.Id}");
    }
    else
    {
        Console.WriteLine($"-> Loi tu may chu (Ma {(int)response.StatusCode}): Ten khong hop le!");
    }
}

public record TaskItem(int Id, string Title, bool IsDone);
public record CreateTaskRequest(string Title);
public record UpdateTaskRequest(bool IsDone);