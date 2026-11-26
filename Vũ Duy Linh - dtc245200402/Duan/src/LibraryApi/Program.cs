using System.Text.Json.Serialization;
using LibraryApi.Services;
using Shared.Models;

// ===== Pha 1: chuẩn bị =====
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<LibraryStore>();

// Trả enum dạng chuỗi ("Pending", "Reserved", ...) thay vì số
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// ===== Pha 2: map endpoint và chạy =====

// Kiểm tra API đang chạy
app.MapGet("/", () => Results.Ok("LibraryApi đang chạy"));

// Danh sách người dùng để chọn người đặt mượn
app.MapGet("/users", (LibraryStore store) =>
    Results.Ok(store.GetUsers().Select(ToUserDto)));

// Danh sách sách: điều kiện lọc nằm trong query string (?availableOnly=true/false)
app.MapGet("/books", (string? availableOnly, LibraryStore store) =>
{
    bool? filterAvailable = null;
    if (availableOnly is not null)
    {
        if (!bool.TryParse(availableOnly, out var parsed))
            return Results.BadRequest(new ApiError("INVALID_FILTER",
                $"Giá trị availableOnly '{availableOnly}' không hợp lệ. Dùng: true, false."));
        filterAvailable = parsed;
    }

    return Results.Ok(store.GetBooks(filterAvailable).Select(ToBookDto));
});

// Chi tiết sách: định danh nằm trong path
app.MapGet("/books/{id:int}", (int id, LibraryStore store) =>
{
    var book = store.FindBook(id);
    return book is null
        ? Results.NotFound(new ApiError("BOOK_NOT_FOUND", $"Không tìm thấy sách {id}."))
        : Results.Ok(ToBookDto(book));
});

// Tạo yêu cầu đặt mượn: dữ liệu lấy từ body
app.MapPost("/reservations", (CreateReservationRequest request, LibraryStore store) =>
{
    // Kiểm tra dữ liệu tại biên, trước khi vào nghiệp vụ. Gom đủ lỗi của mọi trường để client sửa một lần.
    var errors = new Dictionary<string, string[]>();

    if (request.UserId <= 0)
        errors["userId"] = ["UserId phải lớn hơn 0."];
    else if (store.FindUser(request.UserId) is null)
        errors["userId"] = [$"Không có người dùng {request.UserId} trong hệ thống."];

    if (request.BookId <= 0)
        errors["bookId"] = ["BookId phải lớn hơn 0."];
    else if (store.FindBook(request.BookId) is null)
        errors["bookId"] = [$"Không có sách {request.BookId} trong thư viện."];

    if (errors.Count > 0)
        return Results.BadRequest(new ApiError("INVALID_RESERVATION", "Dữ liệu đặt mượn không hợp lệ.", errors));

    // Server tự sinh Id, thời gian và trạng thái ban đầu (Pending)
    var reservation = store.AddReservation(request.UserId, request.BookId);

    // 201 Created + header Location + DTO trong body
    return Results.Created($"/reservations/{reservation.Id}", ToReservationDto(reservation, store));
});

// Chi tiết yêu cầu đặt mượn: định danh nằm trong path
app.MapGet("/reservations/{id:int}", (int id, LibraryStore store) =>
{
    var reservation = store.FindReservation(id);
    return reservation is null
        ? Results.NotFound(new ApiError("RESERVATION_NOT_FOUND", $"Không tìm thấy yêu cầu đặt mượn {id}."))
        : Results.Ok(ToReservationDto(reservation, store));
});

// Danh sách yêu cầu đặt mượn: lọc theo trạng thái trong query string (?status=Pending)
app.MapGet("/reservations", (string? status, LibraryStore store) =>
{
    ReservationStatus? statusFilter = null;
    if (status is not null)
    {
        if (!Enum.TryParse<ReservationStatus>(status, true, out var parsed) || !Enum.IsDefined(parsed))
            return Results.BadRequest(new ApiError("INVALID_STATUS",
                $"Trạng thái '{status}' không hợp lệ. Dùng: Pending, Reserved, Rejected, Borrowed, Returned."));
        statusFilter = parsed;
    }

    return Results.Ok(store.GetReservations(statusFilter).Select(r => ToReservationDto(r, store)));
});

// Danh sách sách đang mượn hoặc đã đặt của một người dùng
app.MapGet("/users/{userId:int}/reservations", (int userId, LibraryStore store) =>
{
    if (store.FindUser(userId) is null)
        return Results.NotFound(new ApiError("USER_NOT_FOUND", $"Không tìm thấy người dùng {userId}."));

    return Results.Ok(store.GetReservationsByUser(userId).Select(r => ToReservationDto(r, store)));
});

// Xác nhận mượn sách (Reserved -> Borrowed)
app.MapPut("/reservations/{id:int}/borrow", (int id, LibraryStore store) =>
{
    var (reservation, errorMessage) = store.TryBorrow(id);
    if (reservation is null)
        return Results.NotFound(new ApiError("RESERVATION_NOT_FOUND", $"Không tìm thấy yêu cầu đặt mượn {id}."));

    if (errorMessage is not null)
        return Results.BadRequest(new ApiError("INVALID_STATUS_TRANSITION", errorMessage));

    return Results.Ok(ToReservationDto(reservation, store));
});

// Trả sách (Borrowed -> Returned)
app.MapPut("/reservations/{id:int}/return", (int id, LibraryStore store) =>
{
    var (reservation, errorMessage) = store.TryReturn(id);
    if (reservation is null)
        return Results.NotFound(new ApiError("RESERVATION_NOT_FOUND", $"Không tìm thấy yêu cầu đặt mượn {id}."));

    if (errorMessage is not null)
        return Results.BadRequest(new ApiError("INVALID_STATUS_TRANSITION", errorMessage));

    return Results.Ok(ToReservationDto(reservation, store));
});

app.Run();

// ===== Chuyển model nội bộ sang DTO =====
static UserDto ToUserDto(User u) => new(u.Id, u.Name, u.Email);

static BookDto ToBookDto(Book b) => new(
    b.Id,
    b.Title,
    b.Author,
    b.ISBN,
    b.Description,
    b.TotalCopies,
    b.AvailableCopies);

static ReservationDto ToReservationDto(Reservation r, LibraryStore store)
{
    var user = store.FindUser(r.UserId);
    var book = store.FindBook(r.BookId);
    return new ReservationDto(
        r.Id,
        r.UserId,
        user?.Name,
        r.BookId,
        book?.Title,
        r.Status,
        r.CreatedAt,
        r.UpdatedAt);
}

// ===== Contracts (DTO & ApiError) =====
record CreateReservationRequest(int UserId, int BookId);
record UserDto(int Id, string Name, string Email);
record BookDto(int Id, string Title, string Author, string ISBN, string? Description, int TotalCopies, int AvailableCopies);
record ReservationDto(int Id, int UserId, string? UserName, int BookId, string? BookTitle, ReservationStatus Status, DateTime CreatedAt, DateTime? UpdatedAt);
record ApiError(string Code, string Message, Dictionary<string, string[]>? Errors = null);
