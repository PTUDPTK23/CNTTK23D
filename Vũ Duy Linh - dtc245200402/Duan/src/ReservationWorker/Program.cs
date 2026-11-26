using Microsoft.EntityFrameworkCore;
using ReservationWorker.Services;
using Shared.Data;

var builder = Host.CreateApplicationBuilder(args);

// Database - PostgreSQL
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Worker Service - xử lý yêu cầu đặt mượn từ RabbitMQ
builder.Services.AddHostedService<ReservationProcessorService>();

var host = builder.Build();

// Khởi tạo Database
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (int i = 1; i <= 15; i++)
    {
        try
        {
            logger.LogInformation("Worker: Đang kết nối Database (lần {Attempt})...", i);
            db.Database.EnsureCreated();
            logger.LogInformation("Worker: Database đã sẵn sàng.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Worker: Kết nối Database thất bại (lần {Attempt}/15). Thử lại sau 3 giây...", i);
            if (i == 15) throw;
            Thread.Sleep(3000);
        }
    }
}

host.Run();
