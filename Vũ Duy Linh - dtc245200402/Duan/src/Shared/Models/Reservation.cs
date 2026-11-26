namespace Shared.Models;

public class Reservation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }
    public Book? Book { get; set; }
}
