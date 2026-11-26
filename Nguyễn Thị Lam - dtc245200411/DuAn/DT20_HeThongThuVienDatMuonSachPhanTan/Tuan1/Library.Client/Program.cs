using System.Net.Http.Json;

var baseUrl = args.FirstOrDefault()
    ?? Environment.GetEnvironmentVariable("LIBRARY_API_URL")
    ?? "http://localhost:8080";

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine("Library distributed client");
while (true)
{
    Console.WriteLine("\n1. List books");
    Console.WriteLine("2. Reserve a book");
    Console.WriteLine("3. Check reservation");
    Console.WriteLine("4. List user reservations");
    Console.WriteLine("0. Exit");
    Console.Write("Choose: ");

    switch (Console.ReadLine())
    {
        case "1":
            await PrintJsonAsync(http.GetAsync("books"));
            break;
        case "2":
            Console.Write("User ID (u1/u2): ");
            var userId = Console.ReadLine()!;
            Console.Write("Book ID: ");
            var bookId = Console.ReadLine()!;
            await PrintJsonAsync(http.PostAsJsonAsync("reservations",
                new { UserId = userId, BookId = Guid.Parse(bookId) }));
            break;
        case "3":
            Console.Write("Reservation ID: ");
            await PrintJsonAsync(http.GetAsync($"reservations/{Console.ReadLine()}"));
            break;
        case "4":
            Console.Write("User ID: ");
            await PrintJsonAsync(http.GetAsync($"users/{Console.ReadLine()}/reservations"));
            break;
        case "0":
            return;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}

static async Task PrintJsonAsync(Task<HttpResponseMessage> requestTask)
{
    using var response = await requestTask;
    Console.WriteLine($"{(int)response.StatusCode} {response.ReasonPhrase}");
    Console.WriteLine(await response.Content.ReadAsStringAsync());
}
