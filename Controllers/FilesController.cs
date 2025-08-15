using FileViewer.Api.Models;
using FileViewer.Api.Services;
using FileViewer.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileViewer.Api.Controllers;

// Test change for GitHub CI/CD pipeline verification
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ApplicationDbContext _context;

    public FilesController(IFileService fileService, ApplicationDbContext context)
    {
        _fileService = fileService;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<FileItem>>> GetFiles([FromQuery] string? directoryPath = null)
    {
        try
        {
            var files = await _fileService.GetFilesAsync(directoryPath ?? "");
            return Ok(files);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error retrieving files", Error = ex.Message });
        }
    }

    [HttpGet("directory")]
    public async Task<ActionResult<DirectoryItem>> GetDirectoryStructure([FromQuery] string? directoryPath = null)
    {
        try
        {
            var structure = await _fileService.GetDirectoryStructureAsync(directoryPath ?? "");
            return Ok(structure);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error retrieving directory structure", Error = ex.Message });
        }
    }

    [HttpPost("upload")]
    [RequestSizeLimit(1073741824)] // 1GB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 1073741824)] // 1GB limit
    public async Task<ActionResult<UploadResponse>> UploadFile([FromForm] IFormFile file, [FromForm] string? directoryPath = null)
    {
        Console.WriteLine($"Upload request received: file={file?.FileName}, directory={directoryPath}");
        
        if (file == null || file.Length == 0)
        {
            Console.WriteLine("No file provided in upload request");
            return BadRequest(new UploadResponse
            {
                Success = false,
                Message = "No file provided"
            });
        }

        try
        {
            var result = await _fileService.UploadFileAsync(file, directoryPath);
            Console.WriteLine($"Upload result: Success={result.Success}, Message={result.Message}");
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upload exception: {ex.Message}");
            return StatusCode(500, new UploadResponse
            {
                Success = false,
                Message = $"Upload failed: {ex.Message}"
            });
        }
    }

    [HttpDelete("{*filePath}")]
    public async Task<ActionResult> DeleteFile(string filePath)
    {
        try
        {
            var result = await _fileService.DeleteFileAsync(filePath);
            if (result)
            {
                return Ok(new { Message = "File deleted successfully" });
            }
            else
            {
                return NotFound(new { Message = "File not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error deleting file", Error = ex.Message });
        }
    }

    [HttpGet("download/{*filePath}")]
    public async Task<ActionResult> GetDownloadUrl(string filePath)
    {
        Console.WriteLine($"Download URL request received: filePath={filePath}");
        try
        {
            var url = await _fileService.GetDownloadUrlAsync(filePath);
            Console.WriteLine($"Generated download URL: {url.Substring(0, Math.Min(100, url.Length))}...");
            return Ok(new { downloadUrl = url });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download URL error: {ex.Message}");
            return StatusCode(500, new { Message = "Error generating download URL", Error = ex.Message });
        }
    }

    [HttpGet("view/{*filePath}")]
    [AllowAnonymous] // Allow access without authentication for file viewing
    public async Task<ActionResult> ViewFile(string filePath)
    {
        Console.WriteLine($"View file request received: filePath={filePath}");
        try
        {
            // Get the actual file content and content type
            var (content, contentType) = await _fileService.GetFileContentWithTypeAsync(filePath);
            
            if (content == null || content.Length == 0)
            {
                return NotFound("File not found");
            }

            // Extract filename from the path
            string fileName = Path.GetFileName(filePath);
            
            // Encode filename properly for Content-Disposition header
            var encodedFileName = Uri.EscapeDataString(fileName);

            // Set headers for inline viewing in browser
            Response.Headers.Add("Content-Disposition", $"inline; filename*=UTF-8''{encodedFileName}");
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "0");
            
            // Return the actual file content with proper content type
            return File(content, contentType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"View file error: {ex.Message}");
            return StatusCode(500, new { Message = "Error viewing file", Error = ex.Message });
        }
    }

    [HttpGet("download-file/{*filePath}")]
    [AllowAnonymous] // Allow access without authentication for file download
    public async Task<ActionResult> DownloadFile(string filePath)
    {
        Console.WriteLine($"Download file request received: filePath={filePath}");
        try
        {
            // Get the actual file content and content type
            var (content, contentType) = await _fileService.GetFileContentWithTypeAsync(filePath);
            
            if (content == null || content.Length == 0)
            {
                return NotFound("File not found");
            }

            // Extract filename from the path
            string fileName = Path.GetFileName(filePath);
            var encodedFileName = Uri.EscapeDataString(fileName);

            // Set headers for download (attachment)
            Response.Headers.Add("Content-Disposition", $"attachment; filename*=UTF-8''{encodedFileName}");
            
            // Return the actual file content with proper content type
            return File(content, contentType, fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download file error: {ex.Message}");
            return StatusCode(500, new { Message = "Error downloading file", Error = ex.Message });
        }
    }    private byte[] CreateMockPdfContent(FileItem file)
    {
        // Create a minimal valid PDF structure
        var pdfContent = $@"%PDF-1.4
1 0 obj
<<
/Type /Catalog
/Pages 2 0 R
>>
endobj

2 0 obj
<<
/Type /Pages
/Kids [3 0 R]
/Count 1
>>
endobj

3 0 obj
<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 612 792]
/Contents 4 0 R
/Resources <<
/Font <<
/F1 5 0 R
>>
>>
>>
endobj

4 0 obj
<<
/Length 150
>>
stream
BT
/F1 24 Tf
72 720 Td
(Mock PDF: {file.Name}) Tj
0 -50 Td
/F1 12 Tf
(Size: {file.Size} bytes) Tj
0 -20 Td
(Created: {file.LastModified}) Tj
0 -30 Td
(This is a mock PDF for development.) Tj
ET
endstream
endobj

5 0 obj
<<
/Type /Font
/Subtype /Type1
/BaseFont /Helvetica
>>
endobj

xref
0 6
0000000000 65535 f 
0000000015 00000 n 
0000000074 00000 n 
0000000120 00000 n 
0000000274 00000 n 
0000000474 00000 n 
trailer
<<
/Size 6
/Root 1 0 R
>>
startxref
551
%%EOF";

        return System.Text.Encoding.UTF8.GetBytes(pdfContent);
    }

        [HttpPost("create-directory")]
    public async Task<ActionResult> CreateDirectory([FromBody] CreateDirectoryRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { Message = "Request is required" });
            }
            
            if (string.IsNullOrWhiteSpace(request.DirectoryPath))
            {
                return BadRequest(new { Message = "Directory path is required" });
            }

            // Ensure the directory path doesn't start with a leading slash
            var directoryPath = request.DirectoryPath.TrimStart('/');
            
            // Try to create the directory
            var success = await _fileService.CreateDirectoryAsync(directoryPath);
            
            if (success)
            {
                return Ok(new { Message = "Directory created successfully" });
            }
            else
            {
                return BadRequest(new { Message = "Failed to create directory. Directory may already exist." });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error creating directory", Error = ex.Message });
        }
    }

    [HttpPut("rename-file")]
    public async Task<ActionResult> RenameFile([FromBody] RenameFileRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OldFilePath) || string.IsNullOrWhiteSpace(request.NewFileName))
            {
                return BadRequest(new { Message = "Old file path and new file name are required" });
            }

            var success = await _fileService.RenameFileAsync(request.OldFilePath, request.NewFileName);
            
            if (success)
            {
                return Ok(new { Message = "File renamed successfully" });
            }
            else
            {
                return BadRequest(new { Message = "Failed to rename file. File may not exist or new name may already be in use." });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error renaming file", Error = ex.Message });
        }
    }

    [HttpPut("rename-directory")]
    public async Task<ActionResult> RenameDirectory([FromBody] RenameDirectoryRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OldDirectoryPath) || string.IsNullOrWhiteSpace(request.NewDirectoryName))
            {
                return BadRequest(new { Message = "Old directory path and new directory name are required" });
            }

            var success = await _fileService.RenameDirectoryAsync(request.OldDirectoryPath, request.NewDirectoryName);
            
            if (success)
            {
                return Ok(new { Message = "Directory renamed successfully" });
            }
            else
            {
                return BadRequest(new { Message = "Failed to rename directory. Directory may not exist or new name may already be in use." });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error renaming directory", Error = ex.Message });
        }
    }

    [HttpDelete("directory/{*directoryPath}")]
    public async Task<ActionResult> DeleteDirectory(string directoryPath)
    {
        Console.WriteLine($"Delete directory request: {directoryPath}");
        
        if (string.IsNullOrEmpty(directoryPath))
        {
            Console.WriteLine("Directory path is empty");
            return BadRequest(new { Message = "Directory path is required" });
        }

        try
        {
            var result = await _fileService.DeleteDirectoryAsync(directoryPath);
            Console.WriteLine($"Delete directory result: {result}");
            if (result)
            {
                return Ok(new { Message = "Directory deleted successfully" });
            }
            else
            {
                return NotFound(new { Message = "Directory not found" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete directory exception: {ex.Message}");
            return StatusCode(500, new { Message = "Error deleting directory", Error = ex.Message });
        }
    }

    [HttpPut("move-file")]
    public async Task<ActionResult> MoveFile([FromBody] MoveFileRequest request)
    {
        if (string.IsNullOrEmpty(request.SourceFilePath) || request.DestinationDirectoryPath == null)
        {
            return BadRequest(new { Message = "Source file path and destination directory path are required" });
        }

        try
        {
            var result = await _fileService.MoveFileAsync(request.SourceFilePath, request.DestinationDirectoryPath);
            if (result)
            {
                return Ok(new { Message = "File moved successfully" });
            }
            else
            {
                return NotFound(new { Message = "File not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error moving file", Error = ex.Message });
        }
    }

    [HttpPut("move-directory")]
    public async Task<ActionResult> MoveDirectory([FromBody] MoveDirectoryRequest request)
    {
        if (string.IsNullOrEmpty(request.SourceDirectoryPath) || request.DestinationDirectoryPath == null)
        {
            return BadRequest(new { Message = "Source directory path and destination directory path are required" });
        }

        try
        {
            var result = await _fileService.MoveDirectoryAsync(request.SourceDirectoryPath, request.DestinationDirectoryPath);
            if (result)
            {
                return Ok(new { Message = "Directory moved successfully" });
            }
            else
            {
                return NotFound(new { Message = "Directory not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error moving directory", Error = ex.Message });
        }
    }

    // TEMPORARY ENDPOINT - REMOVE AFTER USE
    [HttpPost("admin/clear-database")]
    public async Task<ActionResult> ClearDatabase([FromBody] ClearDatabaseRequest request)
    {
        // Security check - only allow with specific confirmation code
        if (request.ConfirmationCode != "CLEAR_ALL_DATA_2025")
        {
            return Unauthorized("Invalid confirmation code");
        }

        try
        {
            var result = await _fileService.ClearAllDataAsync();
            if (result)
            {
                return Ok(new { Message = "Database cleared successfully" });
            }
            else
            {
                return StatusCode(500, new { Message = "Failed to clear database" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error clearing database", Error = ex.Message });
        }
    }

    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Test()
    {
        return Ok(new { Message = "Files API is working", Timestamp = DateTime.UtcNow });
    }
}

public class CreateDirectoryRequest
{
    public string DirectoryPath { get; set; } = string.Empty;
}

public class RenameFileRequest
{
    public string OldFilePath { get; set; } = string.Empty;
    public string NewFileName { get; set; } = string.Empty;
}

public class RenameDirectoryRequest
{
    public string OldDirectoryPath { get; set; } = string.Empty;
    public string NewDirectoryName { get; set; } = string.Empty;
}

public class MoveFileRequest
{
    public string SourceFilePath { get; set; } = string.Empty;
    public string DestinationDirectoryPath { get; set; } = string.Empty;
}

public class MoveDirectoryRequest
{
    public string SourceDirectoryPath { get; set; } = string.Empty;
    public string DestinationDirectoryPath { get; set; } = string.Empty;
}

public class ClearDatabaseRequest
{
    public string ConfirmationCode { get; set; } = string.Empty;
}
