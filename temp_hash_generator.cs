using System.Security.Cryptography;
using System.Text;

string password = "admin123";
string salt = "salt";
using var sha256 = SHA256.Create();
var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
var hash = Convert.ToBase64String(hashedBytes);
Console.WriteLine($"Password hash for '{password}': {hash}");
