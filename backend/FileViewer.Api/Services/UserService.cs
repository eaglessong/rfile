using FileViewer.Api.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FileViewer.Api.Services;

public interface IUserService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByUsernameAsync(string username);
    string GenerateJwtToken(User user);
}

public class UserService : IUserService
{
    private readonly IConfiguration _configuration;
    private readonly List<User> _users; // In-memory storage for demo - use proper database in production

    public UserService(IConfiguration configuration)
    {
        _configuration = configuration;
        _users = new List<User>
        {
            // Default owner user for demo
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@fileviewer.com",
                PasswordHash = HashPassword("admin123"),
                Role = UserRole.Owner,
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        await Task.Delay(1); // Simulate async operation

        var user = _users.FirstOrDefault(u => u.Username == request.Username);
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid username or password"
            };
        }

        var token = GenerateJwtToken(user);
        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            Token = token,
            User = new User
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
                // Don't return password hash
            }
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        await Task.Delay(1); // Simulate async operation

        if (_users.Any(u => u.Username == request.Username))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Username already exists"
            };
        }

        if (_users.Any(u => u.Email == request.Email))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email already exists"
            };
        }

        var newUser = new User
        {
            Id = _users.Count + 1,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = UserRole.Friend, // New users default to Friend role
            CreatedAt = DateTime.UtcNow
        };

        _users.Add(newUser);

        var token = GenerateJwtToken(newUser);
        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            Token = token,
            User = new User
            {
                Id = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                Role = newUser.Role,
                CreatedAt = newUser.CreatedAt
                // Don't return password hash
            }
        };
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        await Task.Delay(1); // Simulate async operation
        return _users.FirstOrDefault(u => u.Id == userId);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        await Task.Delay(1); // Simulate async operation
        return _users.FirstOrDefault(u => u.Username == username);
    }

    public string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["JWT_SECRET_KEY"] ?? "super-secret-key-for-development-only-change-in-production";
        var key = Encoding.ASCII.GetBytes(jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        var passwordHash = HashPassword(password);
        return passwordHash == hash;
    }
}
