namespace LibrarySystem.Models;

/// <summary>
/// Trạng thái của yêu cầu đặt mượn sách.
/// </summary>
public enum ReservationStatus
{
    Pending,
    Reserved,
    Rejected,
    Borrowed,
    Returned
}

/// <summary>
/// Đại diện cho yêu cầu đặt mượn sách.
/// </summary>
public class Reservation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
