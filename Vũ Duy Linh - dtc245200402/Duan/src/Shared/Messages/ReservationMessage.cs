namespace Shared.Messages;

public class ReservationMessage
{
    public int ReservationId { get; set; }
    public int BookId { get; set; }
    public int UserId { get; set; }
}
