namespace FileViewer.Api.Models;

public class FileItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsDirectory { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class DirectoryItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }
    public List<FileItem> Files { get; set; } = new();
    public List<DirectoryItem> Subdirectories { get; set; } = new();
}

public class UploadFileRequest
{
    public IFormFile File { get; set; } = null!;
    public string? DirectoryPath { get; set; }
}

public class UploadResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FileItem? FileInfo { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Guest;
    public DateTime CreatedAt { get; set; }
}

public enum UserRole
{
    Guest = 0,
    Friend = 1,
    Owner = 2
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public User? User { get; set; }
}
