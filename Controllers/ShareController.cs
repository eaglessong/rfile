using FileViewer.Api.Models;
using FileViewer.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileViewer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShareController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IConfiguration _configuration;

    public ShareController(IFileService fileService, IConfiguration configuration)
    {
        _fileService = fileService;
        _configuration = configuration;
    }

    [HttpPost("generate-link")]
    public async Task<ActionResult<ShareLinkResponse>> GenerateShareLink([FromBody] ShareLinkRequest request)
    {
        try
        {
            // Validate that the file exists
            var directoryStructure = await _fileService.GetDirectoryStructureAsync(request.DirectoryPath ?? "");
            var file = FindFileInStructure(directoryStructure, request.FilePath);
            
            if (file == null)
            {
                return NotFound(new { Message = "File not found" });
            }

            // Generate a unique share token
            var shareToken = Guid.NewGuid().ToString("N");
            
            // Store the share link mapping (in a real app, you'd store this in a database)
            // For now, we'll create a simple token-based system
            var baseUrl = GetBaseUrl();
            
            // Use a different route pattern to avoid SPA routing conflicts
            // In a production system, you would store this mapping:
            // shareToken -> { filePath, expiryDate, permissions, etc. }
            // For this demo, we'll encode the file path in the token (not secure for production)
            var encodedFilePath = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.FilePath))
                .Replace('+', '-')  // Make URL safe
                .Replace('/', '_')  // Make URL safe
                .Replace("=", "");  // Remove padding
            var shareUrl = $"{baseUrl}/api/share/download/{encodedFilePath}";

            return Ok(new ShareLinkResponse
            {
                Success = true,
                ShareUrl = shareUrl,
                Message = "Share link generated successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Failed to generate share link", Error = ex.Message });
        }
    }

    [HttpGet("download/{token}")]
    [AllowAnonymous] // Allow anonymous access for shared files
    public async Task<ActionResult> GetSharedFile(string token)
    {
        try
        {
            // Decode the file path from the token (simplified approach)
            string filePath;
            try
            {
                // Convert from URL-safe Base64 back to regular Base64
                var base64 = token.Replace('-', '+').Replace('_', '/');
                // Add padding if needed
                while (base64.Length % 4 != 0)
                {
                    base64 += "=";
                }
                filePath = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Invalid share link", Error = ex.Message });
            }

            // Get the file
            var (content, contentType) = await _fileService.GetFileContentWithTypeAsync(filePath);
            if (content == null || content.Length == 0)
            {
                return NotFound(new { Message = $"File not found or empty: {filePath}" });
            }

            // Get file info for proper content type
            var fileName = Path.GetFileName(filePath);
            var encodedFileName = Uri.EscapeDataString(fileName);

            // Set headers for inline viewing (not download)
            Response.Headers.Add("Content-Disposition", $"inline; filename*=UTF-8''{encodedFileName}");
            
            // Return the file content with proper content type for viewing
            return File(content, contentType);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Failed to retrieve shared file", Error = ex.Message });
        }
    }

    private FileItem? FindFileInStructure(DirectoryItem directory, string filePath)
    {
        // Check files in current directory
        var file = directory.Files.FirstOrDefault(f => f.Path == filePath);
        if (file != null) return file;

        // Check subdirectories recursively
        foreach (var subdir in directory.Subdirectories)
        {
            var found = FindFileInStructure(subdir, filePath);
            if (found != null) return found;
        }

        return null;
    }

    private string GetBaseUrl()
    {
        // Use the custom domain for share links, but API calls go to remotefile.azurewebsites.net
        return "https://rfile.jaysong.org";
    }
}

public class ShareLinkRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string? DirectoryPath { get; set; }
}

public class ShareLinkResponse
{
    public bool Success { get; set; }
    public string ShareUrl { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
