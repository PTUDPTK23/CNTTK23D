using System.Net.Http.Json;

var apiUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiUrl) };

try
{
    var books = await http.GetFromJsonAsync<List<BookDto>>("/api/books") ?? [];
    Console.WriteLine("Danh muc sach:");
    foreach (var book in books)
        Console.WriteLine($"- {book.Id}: {book.Title} | {book.Author} | con {book.AvailableCopies} ban");

    var wantedBook = books.FirstOrDefault(b => b.AvailableCopies > 0);
    if (wantedBook is null)
    {
        Console.WriteLine("Khong co sach san sang de dat muon.");
        return;
    }

    var request = new CreateReservationRequest(wantedBook.Id, "SV001", "Nguyen Van A");
    using var response = await http.PostAsJsonAsync("/api/reservations", request);
    response.EnsureSuccessStatusCode();
    var reservation = await response.Content.ReadFromJsonAsync<ReservationDto>()
        ?? throw new InvalidOperationException("API khong tra ve noi dung yeu cau dat muon.");
    Console.WriteLine($"Da dat muon: {reservation.BookTitle}; ma yeu cau: {reservation.Id}; trang thai: {reservation.Status}");

    var readBack = await http.GetFromJsonAsync<ReservationDto>($"/api/reservations/{reservation.Id}");
    Console.WriteLine($"Doc lai yeu cau: {readBack?.Id}; {readBack?.BookTitle}; {readBack?.Status}");
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Khong the giao tiep voi Library.Api tai {apiUrl}: {ex.Message}");
    Environment.ExitCode = 1;
}

record BookDto(int Id, string Title, string Author, string Isbn, string Category, int AvailableCopies);
record CreateReservationRequest(int BookId, string StudentCode, string StudentName);
record ReservationDto(Guid Id, int BookId, string BookTitle, string StudentCode,
    string StudentName, string Status, DateTimeOffset ReservedAt);
