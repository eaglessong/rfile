using System.Security.Cryptography;
using System.Text;

// Simple password hasher matching the UserService implementation
string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
    return Convert.ToBase64String(hashedBytes);
}

// Hash the password "ella1212"
var hashedPassword = HashPassword("ella1212");
Console.WriteLine($"Hashed password for 'ella1212': {hashedPassword}");

// SQL to insert the user
var sql = $@"
INSERT INTO Users (Username, Email, PasswordHash, Role, CreatedAt)
VALUES ('eaglessong', 'eaglessong@example.com', '{hashedPassword}', 2, '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}');
";

Console.WriteLine("\nSQL to create user:");
Console.WriteLine(sql);
