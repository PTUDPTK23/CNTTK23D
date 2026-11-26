using System.Text;
using System.Text.Json;
using Library.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger("ReservationWorker");

var dbOptions = new DbContextOptionsBuilder<LibraryDb>()
    .UseNpgsql(configuration.GetConnectionString("LibraryDatabase"))
    .Options;

while (true)
{
    try
    {
        await using var db = new LibraryDb(dbOptions);
        await db.Database.EnsureCreatedAsync();
        await using var connection = await CreateConnectionAsync(configuration);
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync("reservation.requests", true, false, false);
        await channel.BasicQosAsync(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var correlationId = eventArgs.BasicProperties.CorrelationId ?? Guid.NewGuid().ToString("N");
            try
            {
                var message = JsonSerializer.Deserialize<ReservationCreated>(
                    Encoding.UTF8.GetString(eventArgs.Body.ToArray()));
                if (message is null)
                    throw new InvalidOperationException("Invalid reservation message.");

                await using var workerDb = new LibraryDb(dbOptions);
                await workerDb.Database.BeginTransactionAsync();
                var reservation = await workerDb.Reservations
                    .FirstOrDefaultAsync(x => x.Id == message.ReservationId);
                if (reservation is null)
                {
                    await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                    return;
                }

                if (reservation.Status != ReservationStatus.Pending)
                {
                    await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                    return;
                }

                var book = await workerDb.Books.FirstAsync(x => x.Id == message.BookId);
                if (book.AvailableCopies > 0)
                {
                    book.AvailableCopies--;
                    reservation.Status = ReservationStatus.Reserved;
                }
                else
                {
                    reservation.Status = ReservationStatus.Rejected;
                }

                reservation.UpdatedAt = DateTimeOffset.UtcNow;
                await workerDb.SaveChangesAsync();
                await workerDb.Database.CommitTransactionAsync();
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false);

                logger.LogInformation(
                    "Processed reservation {ReservationId}: {Status}; correlation {CorrelationId}",
                    reservation.Id, reservation.Status, correlationId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed reservation message; correlation {CorrelationId}; requeueing",
                    correlationId);
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync("reservation.requests", false, consumer);
        logger.LogInformation("Reservation worker is listening on reservation.requests");

        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Worker cannot connect. Retrying in 5 seconds...");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}

static async Task<IConnection> CreateConnectionAsync(IConfiguration configuration)
{
    var factory = new ConnectionFactory
    {
        HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
        Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
        UserName = configuration["RabbitMQ:UserName"] ?? "guest",
        Password = configuration["RabbitMQ:Password"] ?? "guest",
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
    };
    return await factory.CreateConnectionAsync();
}

public enum ReservationStatus
{
    Pending,
    Reserved,
    Rejected,
    Borrowed,
    Returned
}

public sealed class LibraryDb : DbContext
{
    public LibraryDb(DbContextOptions<LibraryDb> options) : base(options) { }
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Book> Books => Set<Book>();
}

public class Reservation
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public Guid BookId { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class Book
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}
