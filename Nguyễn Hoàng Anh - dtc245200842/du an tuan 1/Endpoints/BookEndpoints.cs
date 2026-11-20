using LibrarySystem.Stores;

namespace LibrarySystem.Endpoints;

public static class BookEndpoints
{
    public static void MapBookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/books").WithTags("Books");

        // GET /books - Tra cứu danh sách sách
        // Hỗ trợ query: ?search=...&category=...&author=...
        group.MapGet("", (string? search, string? category, string? author, BookStore store) =>
        {
            List<Models.Book> books;

            if (!string.IsNullOrWhiteSpace(search) ||
                !string.IsNullOrWhiteSpace(category) ||
                !string.IsNullOrWhiteSpace(author))
            {
                books = store.SearchBooks(search, category, author);
            }
            else
            {
                books = store.GetBooks();
            }

            return Results.Ok(new
            {
                total = books.Count,
                items = books.Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.Author,
                    b.Category,
                    b.TotalCopies,
                    b.AvailableCopies
                })
            });
        })
        .WithName("GetBooks")
        .WithSummary("Tra cứu danh sách sách");

        // GET /books/{id} - Tra cứu chi tiết sách + số bản khả dụng
        group.MapGet("/{id:int}", (int id, BookStore store) =>
        {
            var book = store.FindBook(id);
            if (book is null)
                return Results.NotFound(new { message = $"Không tìm thấy sách với id={id}" });

            return Results.Ok(book);
        })
        .WithName("GetBookById")
        .WithSummary("Tra cứu chi tiết sách và số bản khả dụng");

        // GET /books/{id}/availability - Tra cứu riêng số bản khả dụng
        group.MapGet("/{id:int}/availability", (int id, BookStore store) =>
        {
            var book = store.FindBook(id);
            if (book is null)
                return Results.NotFound(new { message = $"Không tìm thấy sách với id={id}" });

            return Results.Ok(new
            {
                bookId = book.Id,
                title = book.Title,
                totalCopies = book.TotalCopies,
                availableCopies = book.AvailableCopies,
                isAvailable = book.AvailableCopies > 0
            });
        })
        .WithName("GetBookAvailability")
        .WithSummary("Tra cứu số bản đang khả dụng");
    }
}
