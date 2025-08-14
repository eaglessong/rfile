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
            var shareUrl = $"{baseUrl}/api/share/file/{shareToken}";

            // In a production system, you would store this mapping:
            // shareToken -> { filePath, expiryDate, permissions, etc. }
            // For this demo, we'll encode the file path in the token (not secure for production)
            var encodedFilePath = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.FilePath));
            shareUrl = $"{baseUrl}/api/share/file/{encodedFilePath}";

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

    [HttpGet("file/{token}")]
    [AllowAnonymous] // Allow anonymous access for shared files
    public async Task<ActionResult> GetSharedFile(string token)
    {
        try
        {
            // Decode the file path from the token (simplified approach)
            string filePath;
            try
            {
                filePath = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            }
            catch
            {
                return BadRequest(new { Message = "Invalid share link" });
            }

            // Get the file
            var (content, contentType) = await _fileService.GetFileContentWithTypeAsync(filePath);
            if (content == null || content.Length == 0)
            {
                return NotFound(new { Message = "File not found or empty" });
            }

            // Get file info for proper content type
            var fileName = Path.GetFileName(filePath);

            return File(content, contentType, fileName);
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
