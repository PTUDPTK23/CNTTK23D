using LibrarySystem.Models;

namespace LibrarySystem.Stores;

// Lưu dữ liệu trong bộ nhớ bằng List<T>. Đăng ký Singleton dùng chung cho mọi request.
// Dữ liệu sẽ mất khi dừng API.
public class BookStore
{
    private readonly List<Book> _books =
    [
        new Book { Id = 1, Title = "Dế Mèn Phiêu Lưu Ký", Author = "Tô Hoài", Isbn = "9786041234567", Publisher = "NXB Kim Đồng", PublishedYear = 1941, Category = "Văn học thiếu nhi", Description = "Câu chuyện phiêu lưu của chú dế mèn qua nhiều vùng đất khác nhau.", TotalCopies = 5, AvailableCopies = 3 },
        new Book { Id = 2, Title = "Truyện Kiều", Author = "Nguyễn Du", Isbn = "9786041234574", Publisher = "NXB Văn Học", PublishedYear = 1820, Category = "Văn học cổ điển", Description = "Tác phẩm thơ Nôm nổi tiếng nhất trong lịch sử văn học Việt Nam.", TotalCopies = 3, AvailableCopies = 1 },
        new Book { Id = 3, Title = "Clean Code", Author = "Robert C. Martin", Isbn = "9780132350884", Publisher = "Prentice Hall", PublishedYear = 2008, Category = "Công nghệ thông tin", Description = "A Handbook of Agile Software Craftsmanship.", TotalCopies = 4, AvailableCopies = 4 },
        new Book { Id = 4, Title = "Design Patterns", Author = "Gang of Four", Isbn = "9780201633610", Publisher = "Addison-Wesley", PublishedYear = 1994, Category = "Công nghệ thông tin", Description = "Elements of Reusable Object-Oriented Software.", TotalCopies = 2, AvailableCopies = 0 },
        new Book { Id = 5, Title = "The Pragmatic Programmer", Author = "David Thomas, Andrew Hunt", Isbn = "9780135957059", Publisher = "Addison-Wesley", PublishedYear = 2019, Category = "Công nghệ thông tin", Description = "Your Journey to Mastery, 20th Anniversary Edition.", TotalCopies = 3, AvailableCopies = 2 },
        new Book { Id = 6, Title = "Nhật Ký Trong Tù", Author = "Hồ Chí Minh", Isbn = "9786041234581", Publisher = "NXB Chính Trị Quốc Gia", PublishedYear = 1960, Category = "Văn học", Description = "Tập thơ được sáng tác trong thời gian bị giam giữ tại Trung Quốc.", TotalCopies = 6, AvailableCopies = 5 },
        new Book { Id = 7, Title = "Số Đỏ", Author = "Vũ Trọng Phụng", Isbn = "9786041234598", Publisher = "NXB Văn Học", PublishedYear = 1936, Category = "Tiểu thuyết", Description = "Tiểu thuyết trào phúng nổi tiếng về xã hội Việt Nam thời Pháp thuộc.", TotalCopies = 3, AvailableCopies = 2 },
        new Book { Id = 8, Title = "Introduction to Algorithms", Author = "Thomas H. Cormen", Isbn = "9780262033848", Publisher = "MIT Press", PublishedYear = 2009, Category = "Công nghệ thông tin", Description = "The comprehensive textbook on algorithms, commonly known as CLRS.", TotalCopies = 2, AvailableCopies = 1 },
        new Book { Id = 9, Title = "Tắt Đèn", Author = "Ngô Tất Tố", Isbn = "9786041234604", Publisher = "NXB Văn Học", PublishedYear = 1939, Category = "Văn học hiện thực", Description = "Tiểu thuyết phản ánh đời sống khổ cực của người nông dân Việt Nam trước Cách mạng.", TotalCopies = 4, AvailableCopies = 3 },
        new Book { Id = 10, Title = "Python Crash Course", Author = "Eric Matthes", Isbn = "9781593279288", Publisher = "No Starch Press", PublishedYear = 2019, Category = "Công nghệ thông tin", Description = "A Hands-On, Project-Based Introduction to Programming.", TotalCopies = 5, AvailableCopies = 4 },
    ];

    // Nhiều request có thể chạy cùng lúc nên khóa khi đọc/ghi danh sách
    private readonly object _lock = new();

    /// <summary>
    /// Chức năng 1: Lấy danh sách tất cả sách.
    /// </summary>
    public List<Book> GetBooks()
    {
        lock (_lock)
        {
            return _books.ToList();
        }
    }

    /// <summary>
    /// Chức năng 1: Tìm sách theo ID (chi tiết sách).
    /// </summary>
    public Book? FindBook(int id)
    {
        lock (_lock)
        {
            return _books.FirstOrDefault(b => b.Id == id);
        }
    }

    /// <summary>
    /// Chức năng 1: Tìm kiếm sách theo tên hoặc tác giả.
    /// </summary>
    public List<Book> SearchBooks(string? search, string? category, string? author)
    {
        lock (_lock)
        {
            IEnumerable<Book> query = _books;

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b =>
                    b.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    b.Author.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(b =>
                    b.Category != null &&
                    b.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                query = query.Where(b =>
                    b.Author.Contains(author, StringComparison.OrdinalIgnoreCase));
            }

            return query.ToList();
        }
    }

    /// <summary>
    /// Chức năng 2: Lấy số bản khả dụng của sách.
    /// </summary>
    public int GetAvailableCopies(int bookId)
    {
        lock (_lock)
        {
            var book = _books.FirstOrDefault(b => b.Id == bookId);
            return book?.AvailableCopies ?? -1;
        }
    }
}
