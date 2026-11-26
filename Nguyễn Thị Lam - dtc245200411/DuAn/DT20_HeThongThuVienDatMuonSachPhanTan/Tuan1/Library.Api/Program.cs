using System.Text;
using System.Text.Json;
using Library.Contracts;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LibraryDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LibraryDatabase")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDb>();
    await db.Database.EnsureCreatedAsync();
    await SeedAsync(db);
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/books", async (LibraryDb db) =>
    await db.Books.AsNoTracking()
        .Select(b => new BookDto(b.Id, b.Title, b.Author, b.TotalCopies, b.AvailableCopies))
        .ToListAsync());

app.MapGet("/books/{id:guid}", async (Guid id, LibraryDb db) =>
{
    var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    return book is null
        ? Results.NotFound()
        : Results.Ok(new BookDto(book.Id, book.Title, book.Author, book.TotalCopies, book.AvailableCopies));
});

app.MapPost("/reservations", async (CreateReservationRequest request, LibraryDb db, IConfiguration config, ILoggerFactory loggerFactory) =>
{
    if (string.IsNullOrWhiteSpace(request.UserId))
        return Results.BadRequest("UserId is required.");

    var user = await db.Users.FindAsync(request.UserId);
    if (user is null)
        return Results.BadRequest("User was not found.");

    var book = await db.Books.FindAsync(request.BookId);
    if (book is null)
        return Results.BadRequest("Book was not found.");

    var reservation = new Reservation
    {
        Id = Guid.NewGuid(),
        UserId = request.UserId,
        BookId = request.BookId,
        Status = ReservationStatus.Pending,
        CreatedAt = DateTimeOffset.UtcNow
    };
    db.Reservations.Add(reservation);
    await db.SaveChangesAsync();

    var message = new ReservationCreated(
        reservation.Id,
        reservation.UserId,
        reservation.BookId,
        Guid.NewGuid().ToString("N"),
        reservation.CreatedAt);

    await PublishAsync(config, message, loggerFactory.CreateLogger("RabbitPublisher"));
    app.Logger.LogInformation(
        "Reservation {ReservationId} for user {UserId} was queued with correlation {CorrelationId}",
        reservation.Id, reservation.UserId, message.CorrelationId);

    return Results.Accepted($"/reservations/{reservation.Id}", new ReservationDto(
        reservation.Id, reservation.UserId, reservation.BookId,
        reservation.Status.ToString(), reservation.CreatedAt, reservation.UpdatedAt));
});

app.MapGet("/reservations/{id:guid}", async (Guid id, LibraryDb db) =>
{
    var reservation = await db.Reservations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    return reservation is null
        ? Results.NotFound()
        : Results.Ok(ToDto(reservation));
});

app.MapGet("/users/{userId}/reservations", async (string userId, LibraryDb db) =>
{
    var reservations = await db.Reservations.AsNoTracking()
        .Where(x => x.UserId == userId)
        .OrderByDescending(x => x.CreatedAt)
        .Select(x => new ReservationDto(
            x.Id, x.UserId, x.BookId, x.Status.ToString(),
            x.CreatedAt, x.UpdatedAt))
        .ToListAsync();
    return Results.Ok(reservations);
});

app.MapPost("/reservations/{id:guid}/borrow", async (Guid id, LibraryDb db) =>
{
    var reservation = await db.Reservations.FirstOrDefaultAsync(x => x.Id == id);
    if (reservation is null)
        return Results.NotFound();
    if (reservation.Status != ReservationStatus.Reserved)
        return Results.Conflict("Only a reserved request can be borrowed.");

    reservation.Status = ReservationStatus.Borrowed;
    reservation.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(ToDto(reservation));
});

app.MapPost("/reservations/{id:guid}/return", async (Guid id, LibraryDb db) =>
{
    var reservation = await db.Reservations.FirstOrDefaultAsync(x => x.Id == id);
    if (reservation is null)
        return Results.NotFound();
    if (reservation.Status != ReservationStatus.Borrowed)
        return Results.Conflict("Only a borrowed request can be returned.");

    var book = await db.Books.FirstAsync(x => x.Id == reservation.BookId);
    if (book.AvailableCopies < book.TotalCopies)
        book.AvailableCopies++;

    reservation.Status = ReservationStatus.Returned;
    reservation.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(ToDto(reservation));
});

app.Run();

static ReservationDto ToDto(Reservation reservation) =>
    new(reservation.Id, reservation.UserId, reservation.BookId,
        reservation.Status.ToString(), reservation.CreatedAt, reservation.UpdatedAt);

static async Task PublishAsync(IConfiguration config, ReservationCreated message, ILogger logger)
{
    var factory = new ConnectionFactory
    {
        HostName = config["RabbitMQ:HostName"] ?? "localhost",
        Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
        UserName = config["RabbitMQ:UserName"] ?? "guest",
        Password = config["RabbitMQ:Password"] ?? "guest"
    };

    await using var connection = await factory.CreateConnectionAsync();
    await using var channel = await connection.CreateChannelAsync();
    await channel.QueueDeclareAsync("reservation.requests", true, false, false);

    var json = JsonSerializer.Serialize(message);
    var body = Encoding.UTF8.GetBytes(json);
    var properties = new BasicProperties
    {
        Persistent = true,
        CorrelationId = message.CorrelationId,
        ContentType = "application/json"
    };
    await channel.BasicPublishAsync(
        string.Empty, "reservation.requests", false, properties, body);
    logger.LogInformation("Published reservation {ReservationId}", message.ReservationId);
}

static async Task SeedAsync(LibraryDb db)
{
    if (!await db.Users.AnyAsync())
    {
        db.Users.AddRange(
            new User { Id = "u1", Name = "Nguyen Van An" },
            new User { Id = "u2", Name = "Tran Thi Binh" });
    }

    if (!await db.Books.AnyAsync())
    {
        db.Books.AddRange(
            new Book { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Title = "Clean Code", Author = "Robert C. Martin", TotalCopies = 3, AvailableCopies = 3 },
            new Book { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Title = "Distributed Systems", Author = "Maarten van Steen", TotalCopies = 2, AvailableCopies = 2 });
    }

    await db.SaveChangesAsync();
}

record CreateReservationRequest(string UserId, Guid BookId);
record BookDto(Guid Id, string Title, string Author, int TotalCopies, int AvailableCopies);
record ReservationDto(Guid Id, string UserId, Guid BookId, string Status, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

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
    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(x => x.Id);
        modelBuilder.Entity<Book>().HasKey(x => x.Id);
        modelBuilder.Entity<Reservation>().HasKey(x => x.Id);
        modelBuilder.Entity<Reservation>()
            .HasOne(x => x.User).WithMany(x => x.Reservations).HasForeignKey(x => x.UserId);
        modelBuilder.Entity<Reservation>()
            .HasOne(x => x.Book).WithMany(x => x.Reservations).HasForeignKey(x => x.BookId);
    }
}

public class User
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public ICollection<Reservation> Reservations { get; set; } = [];
}

public class Book
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = [];
}

public class Reservation
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public Guid BookId { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public User? User { get; set; }
    public Book? Book { get; set; }
}
