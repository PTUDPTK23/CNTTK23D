namespace LibrarySystem.Models;

/// <summary>
/// Đại diện cho người dùng thư viện.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
