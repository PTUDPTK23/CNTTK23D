using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Data;
using Shared.Messages;
using Shared.Models;

namespace ReservationWorker.Services;

/// <summary>
/// Background service lắng nghe queue RabbitMQ, xử lý yêu cầu đặt mượn sách.
/// Kiểm tra số bản khả dụng và xác nhận (Reserved) hoặc từ chối (Rejected) yêu cầu.
/// </summary>
public class ReservationProcessorService : BackgroundService
{
    private readonly ILogger<ReservationProcessorService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string QueueName = "reservation_requests";

    public ReservationProcessorService(
        ILogger<ReservationProcessorService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationWorker đang khởi động...");

        // Kết nối RabbitMQ với retry
        await ConnectToRabbitMqAsync(stoppingToken);

        if (_channel is null)
        {
            _logger.LogError("Không thể kết nối RabbitMQ. Worker dừng.");
            return;
        }

        // Chỉ xử lý 1 message tại một thời điểm (đảm bảo đúng thứ tự)
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Nhận được thông điệp: {Message}", json);

            try
            {
                var message = JsonSerializer.Deserialize<ReservationMessage>(json);
                if (message != null)
                {
                    await ProcessReservationAsync(message);
                }

                // Acknowledge message sau khi xử lý thành công
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Đã xử lý xong ReservationId={Id}", message?.ReservationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý thông điệp: {Message}", json);
                // Nack và requeue để thử lại
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("ReservationWorker đang lắng nghe queue '{Queue}'...", QueueName);

        // Giữ worker chạy
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    /// <summary>
    /// Xử lý một yêu cầu đặt mượn:
    /// - Kiểm tra số bản khả dụng
    /// - Nếu còn → Reserved, giảm AvailableCopies
    /// - Nếu hết → Rejected
    /// </summary>
    private async Task ProcessReservationAsync(ReservationMessage message)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        // Sử dụng transaction để đảm bảo tính nhất quán
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var reservation = await db.Reservations.FindAsync(message.ReservationId);
            if (reservation is null)
            {
                _logger.LogWarning("Không tìm thấy ReservationId={Id}. Bỏ qua.", message.ReservationId);
                await transaction.CommitAsync();
                return;
            }

            // Chỉ xử lý nếu trạng thái đang là Pending
            if (reservation.Status != ReservationStatus.Pending)
            {
                _logger.LogWarning("ReservationId={Id} không ở trạng thái Pending (hiện tại: {Status}). Bỏ qua.",
                    message.ReservationId, reservation.Status);
                await transaction.CommitAsync();
                return;
            }

            // Kiểm tra số bản sách khả dụng (sử dụng pessimistic lock qua FOR UPDATE)
            var book = await db.Books
                .FromSqlRaw("SELECT * FROM \"Books\" WHERE \"Id\" = {0} FOR UPDATE", message.BookId)
                .FirstOrDefaultAsync();

            if (book is null)
            {
                _logger.LogWarning("Không tìm thấy BookId={Id}. Từ chối yêu cầu.", message.BookId);
                reservation.Status = ReservationStatus.Rejected;
                reservation.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return;
            }

            if (book.AvailableCopies > 0)
            {
                // Còn bản → Chấp nhận
                book.AvailableCopies--;
                reservation.Status = ReservationStatus.Reserved;
                reservation.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "✅ Chấp nhận: ReservationId={Id}, BookId={BookId} ('{Title}'), AvailableCopies còn {Count}",
                    reservation.Id, book.Id, book.Title, book.AvailableCopies);
            }
            else
            {
                // Hết bản → Từ chối
                reservation.Status = ReservationStatus.Rejected;
                reservation.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "❌ Từ chối: ReservationId={Id}, BookId={BookId} ('{Title}') - Hết bản khả dụng",
                    reservation.Id, book.Id, book.Title);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xử lý ReservationId={Id}. Rollback transaction.", message.ReservationId);
            await transaction.RollbackAsync();
            throw; // Re-throw để nack message
        }
    }

    /// <summary>
    /// Kết nối đến RabbitMQ với retry tối đa 20 lần (đợi RabbitMQ khởi động trong Docker).
    /// </summary>
    private async Task ConnectToRabbitMqAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
        };

        for (int attempt = 1; attempt <= 20; attempt++)
        {
            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                _logger.LogInformation("Worker: Đang kết nối RabbitMQ (lần {Attempt})...", attempt);
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation("Worker: Kết nối RabbitMQ thành công.");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker: Kết nối RabbitMQ thất bại (lần {Attempt}/20). Thử lại sau 5 giây...", attempt);
                if (attempt < 20)
                    await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogError("Worker: Không thể kết nối RabbitMQ sau 20 lần thử.");
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationWorker đang dừng...");

        if (_channel != null)
            await _channel.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();

        await base.StopAsync(stoppingToken);
    }
}
