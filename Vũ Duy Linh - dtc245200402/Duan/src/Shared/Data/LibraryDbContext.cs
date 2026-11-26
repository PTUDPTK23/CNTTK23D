using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace Shared.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Reservation -> Book
        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.Book)
            .WithMany()
            .HasForeignKey(r => r.BookId);

        // Reservation -> User
        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId);

        // Store enum as string
        modelBuilder.Entity<Reservation>()
            .Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Index for fast lookup
        modelBuilder.Entity<Reservation>()
            .HasIndex(r => r.UserId);

        modelBuilder.Entity<Book>()
            .HasIndex(r => r.ISBN)
            .IsUnique();

        // ============ Seed Data ============

        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "Nguyễn Văn An", Email = "nva@email.com", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new User { Id = 2, Name = "Trần Thị Bình", Email = "ttb@email.com", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new User { Id = 3, Name = "Lê Văn Cường", Email = "lvc@email.com", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<Book>().HasData(
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
        );
    }
}
