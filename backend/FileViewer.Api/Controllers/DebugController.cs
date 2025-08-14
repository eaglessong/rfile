using FileViewer.Api.Data;
using FileViewer.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FileViewer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DebugController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("users")]
    public async Task<ActionResult> GetUsers()
    {
        try
        {
            var users = await _context.Users.Select(u => new 
            {
                u.Id,
                u.Username,
                u.Email,
                u.Role,
                u.CreatedAt,
                PasswordHashLength = u.PasswordHash.Length
            }).ToListAsync();
            
            return Ok(new { 
                UserCount = users.Count,
                Users = users 
            });
        }
        catch (Exception ex)
        {
            return Ok(new { 
                Error = ex.Message,
                UserCount = 0,
                Users = new object[0]
            });
        }
    }

    [HttpGet("create-owner")]
    public async Task<ActionResult> CreateOwner()
    {
        try
        {
            // Check if owner already exists
            var existingOwner = await _context.Users.FirstOrDefaultAsync(u => u.Username == "owner");
            if (existingOwner != null)
            {
                return Ok(new { Message = "Owner account already exists", Username = "owner" });
            }

            var ownerAccount = new User
            {
                Username = "owner",
                Email = "owner@example.com",
                PasswordHash = HashPassword("owner123"),
                Role = UserRole.Owner,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(ownerAccount);
            await _context.SaveChangesAsync();

            return Ok(new { 
                Message = "Owner account created successfully",
                Username = "owner",
                Password = "owner123"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                Message = "Failed to create owner account",
                Error = ex.Message 
            });
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
        return Convert.ToBase64String(hashedBytes);
    }
}
