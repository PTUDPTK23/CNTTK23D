using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Bật tính năng thông báo thời gian thực (SignalR)
builder.Services.AddSignalR();

var app = builder.Build();

// Nơi lưu danh sách công việc trong RAM
var tasks = new List<TaskItem>();
var nextId = 1;
var lockObj = new object();

// 1. Lấy danh sách toàn bộ công việc
app.MapGet("/api/tasks", () =>
{
    lock (lockObj)
    {
        return Results.Ok(tasks);
    }
});

// 2. Lấy chi tiết 1 công việc theo Id
app.MapGet("/api/tasks/{id:int}", (int id) =>
{
    lock (lockObj)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        return task is not null ? Results.Ok(task) : Results.NotFound();
    }
});

// 3. Tạo công việc mới & Báo cho tất cả Client
app.MapPost("/api/tasks", async (CreateTaskRequest? request, IHubContext<TaskHub> hubContext) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { message = "Ten cong viec khong duoc de trong!" });
    }

    TaskItem newTask;
    lock (lockObj)
    {
        newTask = new TaskItem(nextId++, request.Title.Trim(), false);
        tasks.Add(newTask);
    }

    await hubContext.Clients.All.SendAsync("TaskCreated", newTask);

    return Results.Created($"/api/tasks/{newTask.Id}", newTask);
});

// 4. Cập nhật trạng thái hoàn thành
app.MapPut("/api/tasks/{id:int}", async (int id, UpdateTaskRequest request, IHubContext<TaskHub> hubContext) =>
{
    TaskItem? updatedTask = null;

    lock (lockObj)
    {
        var index = tasks.FindIndex(t => t.Id == id);
        if (index == -1) return Results.NotFound();

        updatedTask = tasks[index] with { IsDone = request.IsDone };
        tasks[index] = updatedTask;
    }

    await hubContext.Clients.All.SendAsync("TaskUpdated", updatedTask);
    return Results.Ok(updatedTask);
});

// Mở đường dây kết nối cho Client
app.MapHub<TaskHub>("/hubs/tasks");

app.Run();

// Khai báo khuôn mẫu dữ liệu
public record TaskItem(int Id, string Title, bool IsDone);
public record CreateTaskRequest(string Title);
public record UpdateTaskRequest(bool IsDone);
public class TaskHub : Hub { }