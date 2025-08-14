using FileViewer.Api.Models;
using FileViewer.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileViewer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;

    public AdminController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("users")]
    [Authorize(Roles = "Owner")] // Only owners can view users
    public async Task<ActionResult<List<User>>> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            // Remove password hashes from response for security
            foreach (var user in users)
            {
                user.PasswordHash = string.Empty;
            }
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error retrieving users", Error = ex.Message });
        }
    }

    [HttpGet("users/{id}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Remove password hash from response
            user.PasswordHash = string.Empty;
            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error retrieving user", Error = ex.Message });
        }
    }

    [HttpPost("users")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { Message = "Username and password are required" });
            }

            var existingUser = await _userService.GetUserByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "Username already exists" });
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            var success = await _userService.CreateUserAsync(user, request.Password);
            if (!success)
            {
                return StatusCode(500, new { Message = "Failed to create user" });
            }

            // Remove password hash from response
            user.PasswordHash = string.Empty;
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error creating user", Error = ex.Message });
        }
    }

    [HttpPut("users/{id}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var existingUser = await _userService.GetUserByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Update user properties
            existingUser.Username = request.Username ?? existingUser.Username;
            existingUser.Email = request.Email ?? existingUser.Email;
            existingUser.Role = request.Role ?? existingUser.Role;

            var success = await _userService.UpdateUserAsync(existingUser, request.Password);
            if (!success)
            {
                return StatusCode(500, new { Message = "Failed to update user" });
            }

            // Remove password hash from response
            existingUser.PasswordHash = string.Empty;
            return Ok(existingUser);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error updating user", Error = ex.Message });
        }
    }

    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        try
        {
            // Prevent deletion of the last owner
            var users = await _userService.GetAllUsersAsync();
            var userToDelete = users.FirstOrDefault(u => u.Id == id);
            
            if (userToDelete == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            if (userToDelete.Role == UserRole.Owner)
            {
                var ownerCount = users.Count(u => u.Role == UserRole.Owner);
                if (ownerCount <= 1)
                {
                    return BadRequest(new { Message = "Cannot delete the last owner account" });
                }
            }

            var success = await _userService.DeleteUserAsync(id);
            if (!success)
            {
                return StatusCode(500, new { Message = "Failed to delete user" });
            }

            return Ok(new { Message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error deleting user", Error = ex.Message });
        }
    }

    [HttpGet("current-user")]
    public async Task<ActionResult<User>> GetCurrentUser()
    {
        try
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Remove password hash from response
            user.PasswordHash = string.Empty;
            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error retrieving current user", Error = ex.Message });
        }
    }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Guest;
}

public class UpdateUserRequest
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; } // Optional - only update if provided
    public UserRole? Role { get; set; }
}
