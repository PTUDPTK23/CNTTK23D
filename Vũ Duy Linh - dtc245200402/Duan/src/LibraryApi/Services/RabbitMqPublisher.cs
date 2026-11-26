using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.Messages;

namespace LibraryApi.Services;

/// <summary>
/// Service phát hành thông điệp đặt mượn sách vào RabbitMQ.
/// Sử dụng kết nối bền vững với cơ chế retry.
/// </summary>
public class RabbitMqPublisher : IAsyncDisposable
{
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public const string QueueName = "reservation_requests";

    public RabbitMqPublisher(ILogger<RabbitMqPublisher> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Đảm bảo kết nối đến RabbitMQ, tự động retry nếu chưa kết nối được.
    /// </summary>
    private async Task EnsureConnectedAsync()
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            return;

        await _semaphore.WaitAsync();
        try
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
                return;

            // Đóng kết nối cũ nếu có
            if (_channel != null)
            {
                try { await _channel.DisposeAsync(); } catch { }
                _channel = null;
            }
            if (_connection != null)
            {
                try { await _connection.DisposeAsync(); } catch { }
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest"
            };

            // Retry kết nối tối đa 10 lần
            for (int attempt = 1; attempt <= 10; attempt++)
            {
                try
                {
                    _logger.LogInformation("Đang kết nối RabbitMQ (lần {Attempt})...", attempt);
                    _connection = await factory.CreateConnectionAsync();
                    _channel = await _connection.CreateChannelAsync();

                    // Khai báo queue bền vững
                    await _channel.QueueDeclareAsync(
                        queue: QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    _logger.LogInformation("Kết nối RabbitMQ thành công.");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Kết nối RabbitMQ thất bại (lần {Attempt}/10). Thử lại sau 3 giây...", attempt);
                    if (attempt < 10)
                        await Task.Delay(3000);
                }
            }

            throw new InvalidOperationException("Không thể kết nối đến RabbitMQ sau 10 lần thử.");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gửi thông điệp đặt mượn sách vào queue.
    /// </summary>
    public async Task PublishReservationAsync(ReservationMessage message)
    {
        await EnsureConnectedAsync();

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await _channel!.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Đã gửi yêu cầu đặt mượn vào queue: ReservationId={ReservationId}, BookId={BookId}, UserId={UserId}",
            message.ReservationId, message.BookId, message.UserId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
            await _channel.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
    }
}
