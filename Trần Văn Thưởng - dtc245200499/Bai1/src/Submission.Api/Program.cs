var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var books = new List<Book>
{
    new(1, "Clean Code", "Robert C. Martin", "9780132350884", "Lập trình", 3),
    new(2, "The Pragmatic Programmer", "Andrew Hunt, David Thomas", "9780201616224", "Lập trình", 2),
    new(3, "Designing Data-Intensive Applications", "Martin Kleppmann", "9781449373320", "Hệ thống", 1),
    new(4, "Computer Networking: A Top-Down Approach", "James Kurose, Keith Ross", "9780136681557", "Mạng máy tính", 0)
};
var reservations = new List<ReservationResult>();

app.MapGet("/", () => Results.Ok(new { service = "Library.Api", status = "running" }));

app.MapGet("/api/books", (string? q) =>
{
    var result = string.IsNullOrWhiteSpace(q)
        ? books
        : books.Where(b => b.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
            || b.Author.Contains(q, StringComparison.OrdinalIgnoreCase)
            || b.Category.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    return Results.Ok(result);
});

app.MapGet("/api/books/{id:int}", (int id) =>
{
    var book = books.FirstOrDefault(b => b.Id == id);
    return book is null ? Results.NotFound(new { message = $"Không tìm thấy sách id={id}." }) : Results.Ok(book);
});

app.MapPost("/api/reservations", (CreateReservationRequest? request) =>
{
    if (request is null || request.BookId <= 0 || string.IsNullOrWhiteSpace(request.StudentCode)
        || string.IsNullOrWhiteSpace(request.StudentName))
        return Results.BadRequest(new { message = "BookId phải lớn hơn 0; StudentCode và StudentName không được để trống." });

    var book = books.FirstOrDefault(b => b.Id == request.BookId);
    if (book is null)
        return Results.BadRequest(new { message = "BookId không tồn tại trong danh mục." });
    if (book.AvailableCopies <= 0)
        return Results.Conflict(new { message = "Sách hiện đã hết bản có thể mượn." });

    // Xử lý kiểm tra và trừ số bản trong một đoạn đồng bộ để tránh nhận vượt số lượng trong tiến trình này.
    book.AvailableCopies--;
    var reservation = new ReservationResult(Guid.NewGuid(), book.Id, book.Title,
        request.StudentCode.Trim(), request.StudentName.Trim(), "reserved", DateTimeOffset.UtcNow);
    reservations.Add(reservation);
    return Results.Created($"/api/reservations/{reservation.Id}", reservation);
});

app.MapGet("/api/reservations/{id:guid}", (Guid id) =>
{
    var reservation = reservations.FirstOrDefault(r => r.Id == id);
    return reservation is null ? Results.NotFound(new { message = "Không tìm thấy yêu cầu đặt mượn." }) : Results.Ok(reservation);
});

app.Run();

record Book(int Id, string Title, string Author, string Isbn, string Category, int AvailableCopies)
{
    public int AvailableCopies { get; set; } = AvailableCopies;
}
record CreateReservationRequest(int BookId, string StudentCode, string StudentName);
record ReservationResult(Guid Id, int BookId, string BookTitle, string StudentCode,
    string StudentName, string Status, DateTimeOffset ReservedAt);
