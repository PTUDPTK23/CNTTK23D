namespace Library.Contracts;

public record ReservationCreated(
    Guid ReservationId,
    string UserId,
    Guid BookId,
    string CorrelationId,
    DateTimeOffset CreatedAt);

