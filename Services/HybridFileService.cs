using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using FileViewer.Api.Data;
using FileViewer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FileViewer.Api.Services;

/// <summary>
/// HybridFileService provides the best of both worlds:
/// - File content is stored efficiently in Azure Blob Storage (no size increase, scalable)
/// - File metadata is stored in database for fast queries and directory structure
/// - Supports large files without performance issues
/// - Provides secure access through SAS tokens
/// 
/// This is the recommended service for production use.
/// </summary>
public class HybridFileService : IFileService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ApplicationDbContext _context;
    private const string ContainerName = "files";

    public HybridFileService(BlobServiceClient blobServiceClient, ApplicationDbContext context)
    {
        _blobServiceClient = blobServiceClient;
        _context = context;
    }

    public async Task<List<FileItem>> GetFilesAsync(string directoryPath = "")
    {
        var files = await _context.Files
            .Where(f => f.DirectoryId == null || f.Path.StartsWith(directoryPath))
            .Where(f => !f.Name.Equals(".placeholder"))
            .ToListAsync();

        return files;
    }

    public async Task<DirectoryItem> GetDirectoryStructureAsync(string directoryPath = "")
    {
        try
        {
            DirectoryItem structure;

            if (string.IsNullOrEmpty(directoryPath))
            {
                // Get or create root directory structure
                structure = new DirectoryItem
                {
                    Id = 0,
                    Name = "root",
                    Path = "",
                    CreatedDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    ParentDirectoryId = null,
                    Files = new List<FileItem>(),
                    Subdirectories = new List<DirectoryItem>()
                };

                // Get all root files and directories, excluding placeholder files
                structure.Files = await _context.Files
                    .Where(f => f.DirectoryId == null && !f.Name.Equals(".placeholder"))
                    .ToListAsync();

                Console.WriteLine($"Root directory: Found {structure.Files.Count} files");
                foreach (var file in structure.Files)
                {
                    Console.WriteLine($"  File: '{file.Name}' (Path: '{file.Path}', Size: {file.Size})");
                }

                structure.Subdirectories = await _context.Directories
                    .Where(d => d.ParentDirectoryId == null)
                    .ToListAsync();

                // Populate file counts for each subdirectory, excluding placeholder files
                foreach (var subdir in structure.Subdirectories)
                {
                    subdir.Files = await _context.Files
                        .Where(f => f.DirectoryId == subdir.Id && !f.Name.Equals(".placeholder"))
                        .ToListAsync();
                }
            }
            else
            {
                // Get specific directory structure from database
                structure = await _context.Directories
                    .FirstOrDefaultAsync(d => d.Path == directoryPath) ?? new DirectoryItem
                {
                    Id = 0,
                    Name = directoryPath.Split('/').Last(),
                    Path = directoryPath,
                    CreatedDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                if (structure != null)
                {
                    structure.Files = await _context.Files
                        .Where(f => f.DirectoryId == structure.Id && !f.Name.Equals(".placeholder"))
                        .ToListAsync();

                    structure.Subdirectories = await _context.Directories
                        .Where(d => d.ParentDirectoryId == structure.Id)
                        .ToListAsync();

                    // Populate file counts for each subdirectory
                    foreach (var subdir in structure.Subdirectories)
                    {
                        subdir.Files = await _context.Files
                            .Where(f => f.DirectoryId == subdir.Id && !f.Name.Equals(".placeholder"))
                            .ToListAsync();
                    }
                }
            }

            // Calculate total size for the structure
            structure.TotalSize = await CalculateDirectorySize(structure);

            return structure;
        }
        catch (Exception ex)
        {
            // Log the error and return an empty structure
            Console.WriteLine($"Error in GetDirectoryStructureAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Return a safe fallback structure
            return new DirectoryItem
            {
                Id = 0,
                Name = "root",
                Path = directoryPath ?? "",
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                ParentDirectoryId = null,
                Files = new List<FileItem>(),
                Subdirectories = new List<DirectoryItem>()
            };
        }
    }

    private async Task<long> CalculateDirectorySize(DirectoryItem directory)
    {
        long totalSize = 0;

        // Calculate size of files in this directory (excluding placeholder files)
        var filesInDirectory = await _context.Files
            .Where(f => f.DirectoryId == directory.Id && !f.Name.EndsWith(".placeholder"))
            .ToListAsync();
        totalSize += filesInDirectory.Sum(f => f.Size);

        // Calculate size of all subdirectories recursively
        foreach (var subdirectory in directory.Subdirectories)
        {
            subdirectory.TotalSize = await CalculateDirectorySize(subdirectory);
            totalSize += subdirectory.TotalSize;
        }

        return totalSize;
    }

    public async Task<UploadResponse> UploadFileAsync(IFormFile file, string? directoryPath = null)
    {
        try
        {
            Console.WriteLine($"HybridFileService: Uploading file '{file.FileName}' to directory '{directoryPath}'");

            // Upload file content to Azure Blob Storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobPath = string.IsNullOrEmpty(directoryPath) 
                ? file.FileName 
                : $"{directoryPath.TrimEnd('/')}/{file.FileName}";

            var blobClient = containerClient.GetBlobClient(blobPath);

            // Check if blob already exists
            if (await blobClient.ExistsAsync())
            {
                Console.WriteLine($"Blob already exists: {blobPath}");
                return new UploadResponse
                {
                    Success = false,
                    Message = "File already exists"
                };
            }

            // Upload file to blob storage
            using var stream = file.OpenReadStream();
            var blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                }
            };

            await blobClient.UploadAsync(stream, blobUploadOptions);
            Console.WriteLine($"File uploaded to blob storage: {blobPath}");

            // Store metadata in database (without file content)
            var uploadedFile = await StoreFileMetadataAsync(file.FileName, directoryPath ?? "", file.Length, file.ContentType, blobClient.Uri.ToString());
            
            if (uploadedFile == null)
            {
                // If metadata storage failed, clean up the blob
                await blobClient.DeleteIfExistsAsync();
                return new UploadResponse
                {
                    Success = false,
                    Message = "Failed to store file metadata"
                };
            }

            Console.WriteLine($"File metadata stored in database with ID: {uploadedFile.Id}");

            return new UploadResponse
            {
                Success = true,
                Message = "File uploaded successfully",
                FileInfo = uploadedFile
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upload error: {ex.Message}");
            return new UploadResponse
            {
                Success = false,
                Message = $"Upload failed: {ex.Message}"
            };
        }
    }

    private async Task<FileItem?> StoreFileMetadataAsync(string fileName, string directoryPath, long fileSize, string contentType, string blobUrl)
    {
        var filePath = string.IsNullOrEmpty(directoryPath) 
            ? fileName 
            : $"{directoryPath.TrimEnd('/')}/{fileName}";

        Console.WriteLine($"Storing metadata: fileName='{fileName}', directoryPath='{directoryPath}', filePath='{filePath}'");

        // Check if file metadata already exists
        var existingFile = await _context.Files
            .FirstOrDefaultAsync(f => f.Path == filePath);

        if (existingFile != null)
        {
            Console.WriteLine($"File metadata already exists: {filePath}");
            return null;
        }

        // Get parent directory if specified
        DirectoryItem? parentDirectory = null;
        if (!string.IsNullOrEmpty(directoryPath))
        {
            parentDirectory = await _context.Directories
                .FirstOrDefaultAsync(d => d.Path == directoryPath);
            Console.WriteLine($"Parent directory found: {parentDirectory != null} (ID: {parentDirectory?.Id})");
        }

        var fileItem = new FileItem
        {
            Name = fileName,
            Path = filePath,
            Size = fileSize,
            ContentType = contentType,
            CreatedDate = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            IsDirectory = false,
            Url = blobUrl,
            DirectoryId = parentDirectory?.Id,
            FileContentBase64 = "" // Empty since we store in blob storage
        };

        _context.Files.Add(fileItem);
        await _context.SaveChangesAsync();

        return fileItem;
    }

    public async Task<string> GetDownloadUrlAsync(string filePath)
    {
        // Return the blob storage URL for direct download
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(filePath);
        
        // Generate a short-lived SAS token for secure access
        if (blobClient.CanGenerateSasUri)
        {
            var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
            {
                BlobContainerName = ContainerName,
                BlobName = filePath,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // 1 hour expiry
            };
            sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        return blobClient.Uri.ToString();
    }

    public async Task<(byte[] content, string contentType)> GetFileContentWithTypeAsync(string filePath)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = containerClient.GetBlobClient(filePath);

            if (!await blobClient.ExistsAsync())
            {
                return (Array.Empty<byte>(), "application/octet-stream");
            }

            var response = await blobClient.DownloadAsync();
            using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);

            var contentType = response.Value.Details.ContentType ?? "application/octet-stream";
            return (memoryStream.ToArray(), contentType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file content: {ex.Message}");
            return (Array.Empty<byte>(), "application/octet-stream");
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            // Delete from blob storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = containerClient.GetBlobClient(filePath);
            await blobClient.DeleteIfExistsAsync();

            // Delete metadata from database
            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Path == filePath);

            if (file != null)
            {
                _context.Files.Remove(file);
                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting file: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateDirectoryAsync(string directoryPath)
    {
        try
        {
            // Clean and validate the directory path
            directoryPath = directoryPath.Trim().TrimStart('/').Replace('\\', '/');
            
            if (string.IsNullOrEmpty(directoryPath))
            {
                return false;
            }

            // Extract directory name from the full path
            var directoryName = Path.GetFileName(directoryPath.TrimEnd('/'));
            var parentPath = Path.GetDirectoryName(directoryPath.TrimEnd('/'))?.Replace('\\', '/');

            if (string.IsNullOrEmpty(directoryName))
            {
                return false;
            }

            // Check if directory already exists
            var existingDirectory = await _context.Directories
                .FirstOrDefaultAsync(d => d.Path == directoryPath);

            if (existingDirectory != null)
            {
                return false; // Directory already exists
            }

            // Get parent directory if specified
            DirectoryItem? parentDirectory = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                parentDirectory = await _context.Directories
                    .FirstOrDefaultAsync(d => d.Path == parentPath);
            }

            var directory = new DirectoryItem
            {
                Name = directoryName,
                Path = directoryPath,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                ParentDirectoryId = parentDirectory?.Id
            };

            Console.WriteLine($"Creating directory object: Name='{directory.Name}', Path='{directory.Path}'");

            _context.Directories.Add(directory);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Directory saved to database with ID: {directory.Id}");

            // Create a placeholder file in blob storage to ensure the directory exists
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            
            var placeholderPath = $"{directoryPath}/.placeholder";
            var blobClient = containerClient.GetBlobClient(placeholderPath);
            
            Console.WriteLine($"Creating placeholder blob at: {placeholderPath}");
            
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("placeholder"));
            await blobClient.UploadAsync(stream, overwrite: true);

            Console.WriteLine("Directory creation completed successfully");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DeleteDirectoryAsync(string directoryPath)
    {
        try
        {
            // Delete all files in the directory from blob storage and database
            var directory = await _context.Directories
                .Include(d => d.Files)
                .FirstOrDefaultAsync(d => d.Path == directoryPath);

            if (directory == null)
            {
                return false;
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);

            // Delete all files in this directory
            foreach (var file in directory.Files)
            {
                var blobClient = containerClient.GetBlobClient(file.Path);
                await blobClient.DeleteIfExistsAsync();
            }

            // Delete placeholder file
            var placeholderBlobClient = containerClient.GetBlobClient($"{directoryPath}/.placeholder");
            await placeholderBlobClient.DeleteIfExistsAsync();

            // Delete directory and all files from database
            _context.Files.RemoveRange(directory.Files);
            _context.Directories.Remove(directory);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting directory: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RenameFileAsync(string oldFilePath, string newFileName)
    {
        try
        {
            // Update metadata in database
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Path == oldFilePath);
            if (file == null) return false;

            var directory = Path.GetDirectoryName(oldFilePath)?.Replace('\\', '/');
            var newFilePath = string.IsNullOrEmpty(directory) ? newFileName : $"{directory}/{newFileName}";

            // Check if new file already exists
            var existingFile = await _context.Files.FirstOrDefaultAsync(f => f.Path == newFilePath);
            if (existingFile != null) return false;

            // Copy blob to new location
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var oldBlobClient = containerClient.GetBlobClient(oldFilePath);
            var newBlobClient = containerClient.GetBlobClient(newFilePath);

            await newBlobClient.StartCopyFromUriAsync(oldBlobClient.Uri);

            // Wait for copy to complete
            BlobProperties properties;
            do
            {
                await Task.Delay(100);
                properties = await newBlobClient.GetPropertiesAsync();
            } while (properties.CopyStatus == CopyStatus.Pending);

            if (properties.CopyStatus == CopyStatus.Success)
            {
                // Delete old blob
                await oldBlobClient.DeleteIfExistsAsync();

                // Update database metadata
                file.Name = newFileName;
                file.Path = newFilePath;
                file.LastModified = DateTime.UtcNow;
                file.Url = newBlobClient.Uri.ToString();

                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error renaming file: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RenameDirectoryAsync(string oldDirectoryPath, string newDirectoryName)
    {
        // This would require updating all child files and subdirectories
        // Implementation similar to DatabaseFileService but with blob operations
        throw new NotImplementedException("Directory renaming not yet implemented for hybrid service");
    }

    public async Task<bool> MoveFileAsync(string sourceFilePath, string destinationDirectoryPath)
    {
        try
        {
            var fileName = Path.GetFileName(sourceFilePath);
            var newFilePath = string.IsNullOrEmpty(destinationDirectoryPath) 
                ? fileName 
                : $"{destinationDirectoryPath.TrimEnd('/')}/{fileName}";

            // Update metadata in database
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Path == sourceFilePath);
            if (file == null) return false;

            // Check if destination file already exists
            var existingFile = await _context.Files.FirstOrDefaultAsync(f => f.Path == newFilePath);
            if (existingFile != null) return false;

            // Get destination directory
            DirectoryItem? destinationDirectory = null;
            if (!string.IsNullOrEmpty(destinationDirectoryPath))
            {
                destinationDirectory = await _context.Directories
                    .FirstOrDefaultAsync(d => d.Path == destinationDirectoryPath);
            }

            // Move blob in storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var sourceBlobClient = containerClient.GetBlobClient(sourceFilePath);
            var destinationBlobClient = containerClient.GetBlobClient(newFilePath);

            await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

            // Wait for copy to complete
            BlobProperties properties;
            do
            {
                await Task.Delay(100);
                properties = await destinationBlobClient.GetPropertiesAsync();
            } while (properties.CopyStatus == CopyStatus.Pending);

            if (properties.CopyStatus == CopyStatus.Success)
            {
                // Delete source blob
                await sourceBlobClient.DeleteIfExistsAsync();

                // Update database metadata
                file.Path = newFilePath;
                file.DirectoryId = destinationDirectory?.Id;
                file.LastModified = DateTime.UtcNow;
                file.Url = destinationBlobClient.Uri.ToString();

                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error moving file: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> MoveDirectoryAsync(string sourceDirectoryPath, string destinationDirectoryPath)
    {
        // Implementation would be complex - need to move all files and update all references
        throw new NotImplementedException("Directory moving not yet implemented for hybrid service");
    }

    public async Task<bool> ClearAllDataAsync()
    {
        try
        {
            Console.WriteLine("Starting database cleanup...");
            
            // Remove all files from database
            var fileCount = await _context.Files.CountAsync();
            _context.Files.RemoveRange(_context.Files);
            
            // Remove all directories from database
            var directoryCount = await _context.Directories.CountAsync();
            _context.Directories.RemoveRange(_context.Directories);
            
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"Database cleared: {fileCount} files and {directoryCount} directories removed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing database: {ex.Message}");
            return false;
        }
    }
}
