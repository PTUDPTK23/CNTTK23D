using Shared.Models;

namespace LibraryApi.Services;

/// <summary>
/// Lưu trữ dữ liệu sách, người dùng và yêu cầu đặt mượn trong bộ nhớ (giai đoạn Tuần 1).
/// </summary>
public class LibraryStore
{
    private readonly Lock _lock = new();
    private int _nextReservationId = 1;

    private readonly List<User> _users =
    [
        new User { Id = 1, Name = "Nguyễn Văn An", Email = "nva@email.com", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        new User { Id = 2, Name = "Trần Thị Bình", Email = "ttb@email.com", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        new User { Id = 3, Name = "Lê Văn Cường", Email = "lvc@email.com", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
    ];

    private readonly List<Book> _books =
    [
        new Book
        {
            Id = 1, Title = "Clean Code", Author = "Robert C. Martin",
            ISBN = "978-0132350884", Description = "A Handbook of Agile Software Craftsmanship",
            TotalCopies = 3, AvailableCopies = 3,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new Book
        {
            Id = 2, Title = "Design Patterns", Author = "Gang of Four",
            ISBN = "978-0201633610", Description = "Elements of Reusable Object-Oriented Software",
            TotalCopies = 2, AvailableCopies = 2,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new Book
        {
            Id = 3, Title = "The Pragmatic Programmer", Author = "David Thomas, Andrew Hunt",
            ISBN = "978-0135957059", Description = "Your Journey to Mastery",
            TotalCopies = 4, AvailableCopies = 4,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new Book
        {
            Id = 4, Title = "Introduction to Algorithms", Author = "Thomas H. Cormen",
            ISBN = "978-0262033848", Description = "A comprehensive textbook on algorithms",
            TotalCopies = 2, AvailableCopies = 2,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new Book
        {
            Id = 5, Title = "Distributed Systems", Author = "Andrew S. Tanenbaum",
            ISBN = "978-1543057386", Description = "Principles and Paradigms",
            TotalCopies = 3, AvailableCopies = 3,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }
    ];

    private readonly List<Reservation> _reservations = [];

    public IReadOnlyList<User> GetUsers()
    {
        lock (_lock)
            return _users.ToList();
    }

    public User? FindUser(int id)
    {
        lock (_lock)
            return _users.FirstOrDefault(u => u.Id == id);
    }

    public IReadOnlyList<Book> GetBooks(bool? availableOnly = null)
    {
        lock (_lock)
        {
            IEnumerable<Book> query = _books;
            if (availableOnly == true)
                query = query.Where(b => b.AvailableCopies > 0);
            else if (availableOnly == false)
                query = query.Where(b => b.AvailableCopies == 0);

            return query.ToList();
        }
    }

    public Book? FindBook(int id)
    {
        lock (_lock)
            return _books.FirstOrDefault(b => b.Id == id);
    }

    public Reservation AddReservation(int userId, int bookId)
    {
        lock (_lock)
        {
            var reservation = new Reservation
            {
                Id = _nextReservationId++,
                UserId = userId,
                BookId = bookId,
                Status = ReservationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _reservations.Add(reservation);
            return reservation;
        }
    }

    public Reservation? FindReservation(int id)
    {
        lock (_lock)
            return _reservations.FirstOrDefault(r => r.Id == id);
    }

    public IReadOnlyList<Reservation> GetReservations(ReservationStatus? status = null)
    {
        lock (_lock)
        {
            IEnumerable<Reservation> query = _reservations;
            if (status is not null)
                query = query.Where(r => r.Status == status.Value);

            return query.OrderByDescending(r => r.CreatedAt).ToList();
        }
    }

    public IReadOnlyList<Reservation> GetReservationsByUser(int userId)
    {
        lock (_lock)
            return _reservations
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
    }

    public (Reservation? Reservation, string? ErrorMessage) TryBorrow(int id)
    {
        lock (_lock)
        {
            var reservation = _reservations.FirstOrDefault(r => r.Id == id);
            if (reservation is null)
                return (null, null);

            if (reservation.Status != ReservationStatus.Reserved)
                return (reservation, $"Chỉ có thể mượn sách khi trạng thái là Reserved. Trạng thái hiện tại: {reservation.Status}");

            reservation.Status = ReservationStatus.Borrowed;
            reservation.UpdatedAt = DateTime.UtcNow;
            return (reservation, null);
        }
    }

    public (Reservation? Reservation, string? ErrorMessage) TryReturn(int id)
    {
        lock (_lock)
        {
            var reservation = _reservations.FirstOrDefault(r => r.Id == id);
            if (reservation is null)
                return (null, null);

            if (reservation.Status != ReservationStatus.Borrowed)
                return (reservation, $"Chỉ có thể trả sách khi trạng thái là Borrowed. Trạng thái hiện tại: {reservation.Status}");

            reservation.Status = ReservationStatus.Returned;
            reservation.UpdatedAt = DateTime.UtcNow;

            var book = _books.FirstOrDefault(b => b.Id == reservation.BookId);
            if (book is not null)
                book.AvailableCopies++;

            return (reservation, null);
        }
    }
}
