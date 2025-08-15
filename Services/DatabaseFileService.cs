using FileViewer.Api.Data;
using FileViewer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FileViewer.Api.Services;

public class DatabaseFileService : IFileService
{
    private readonly ApplicationDbContext _context;

    public DatabaseFileService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FileItem>> GetFilesAsync(string directoryPath = "")
    {
        try
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                // Get root files, excluding placeholder files
                return await _context.Files
                    .Where(f => f.DirectoryId == null && !f.Name.Equals(".placeholder"))
                    .ToListAsync();
            }
            else
            {
                // Get files in specific directory, excluding placeholder files
                var directory = await _context.Directories
                    .FirstOrDefaultAsync(d => d.Path == directoryPath);
                
                if (directory == null)
                {
                    return new List<FileItem>();
                }

                return await _context.Files
                    .Where(f => f.DirectoryId == directory.Id && !f.Name.Equals(".placeholder"))
                    .ToListAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetFilesAsync: {ex.Message}");
            return new List<FileItem>();
        }
    }

    public async Task<List<DirectoryItem>> GetDirectoriesAsync(string directoryPath = "")
    {
        var directories = await _context.Directories
            .Where(d => d.Path.StartsWith(directoryPath))
            .Include(d => d.Files)
            .Include(d => d.Subdirectories)
            .ToListAsync();

        // Build the directory structure
        var result = new List<DirectoryItem>();
        foreach (var dir in directories)
        {
            // Get files in this directory
            dir.Files = await _context.Files
                .Where(f => f.DirectoryId == dir.Id)
                .ToListAsync();

            // Get subdirectories
            dir.Subdirectories = await _context.Directories
                .Where(d => d.ParentDirectoryId == dir.Id)
                .ToListAsync();

            result.Add(dir);
        }

        return result;
    }

    private async Task<long> CalculateDirectorySize(DirectoryItem directory)
    {
        long totalSize = 0;

        // Calculate size of files in this directory
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

    public async Task<DirectoryItem> GetDirectoryStructureAsync(string directoryPath = "")
    {
        try
        {
            DirectoryItem structure;

            if (string.IsNullOrEmpty(directoryPath))
            {
                // Root directory
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
                // Get the current directory
                structure = await _context.Directories
                    .FirstOrDefaultAsync(d => d.Path == directoryPath);

                if (structure == null)
                {
                    // Directory not found, return empty structure
                    structure = new DirectoryItem 
                    { 
                        Id = 0,
                        Name = "Not Found", 
                        Path = directoryPath,
                        CreatedDate = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        ParentDirectoryId = null,
                        Files = new List<FileItem>(),
                        Subdirectories = new List<DirectoryItem>()
                    };
                }
                else
                {
                    // Load files and subdirectories for this directory, excluding placeholder files
                    structure.Files = await _context.Files
                        .Where(f => f.DirectoryId == structure.Id && !f.Name.Equals(".placeholder"))
                        .ToListAsync();

                    structure.Subdirectories = await _context.Directories
                        .Where(d => d.ParentDirectoryId == structure.Id)
                        .ToListAsync();

                    // Populate file counts for each subdirectory, excluding placeholder files
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

    public async Task<byte[]> GetFileContentAsync(string filePath)
    {
        var file = await _context.Files
            .FirstOrDefaultAsync(f => f.Path == filePath);

        if (file == null || string.IsNullOrEmpty(file.FileContentBase64))
        {
            return Array.Empty<byte>();
        }

        try
        {
            return Convert.FromBase64String(file.FileContentBase64);
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    public async Task<UploadResponse> UploadFileAsync(IFormFile file, string? directoryPath = null)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var content = memoryStream.ToArray();

            var uploadedFile = await UploadFileInternalAsync(file.FileName, directoryPath ?? "", content, file.ContentType);
            
            if (uploadedFile == null)
            {
                return new UploadResponse
                {
                    Success = false,
                    Message = "File already exists"
                };
            }

            return new UploadResponse
            {
                Success = true,
                Message = "File uploaded successfully",
                FileInfo = uploadedFile
            };
        }
        catch (Exception ex)
        {
            return new UploadResponse
            {
                Success = false,
                Message = $"Upload failed: {ex.Message}"
            };
        }
    }

    public async Task<string> GetDownloadUrlAsync(string filePath)
    {
        // For database storage, we'll return a direct download endpoint
        return $"/api/files/download?path={Uri.EscapeDataString(filePath)}";
    }

    public async Task<(byte[] content, string contentType)> GetFileContentWithTypeAsync(string filePath)
    {
        var file = await _context.Files
            .FirstOrDefaultAsync(f => f.Path == filePath);

        if (file == null || string.IsNullOrEmpty(file.FileContentBase64))
        {
            return (Array.Empty<byte>(), "application/octet-stream");
        }

        try
        {
            var content = Convert.FromBase64String(file.FileContentBase64);
            return (content, file.ContentType);
        }
        catch
        {
            return (Array.Empty<byte>(), "application/octet-stream");
        }
    }

    private async Task<FileItem?> UploadFileInternalAsync(string fileName, string directoryPath, byte[] content, string contentType)
    {
        var filePath = string.IsNullOrEmpty(directoryPath) 
            ? fileName 
            : $"{directoryPath.TrimEnd('/')}/{fileName}";

        // Check if file already exists
        var existingFile = await _context.Files
            .FirstOrDefaultAsync(f => f.Path == filePath);

        if (existingFile != null)
        {
            return null; // File already exists
        }

        // Get parent directory if specified
        DirectoryItem? parentDirectory = null;
        if (!string.IsNullOrEmpty(directoryPath))
        {
            parentDirectory = await _context.Directories
                .FirstOrDefaultAsync(d => d.Path == directoryPath);
        }

        var fileItem = new FileItem
        {
            Name = fileName,
            Path = filePath,
            Size = content.Length,
            ContentType = contentType,
            CreatedDate = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            IsDirectory = false,
            Url = "#",
            DirectoryId = parentDirectory?.Id,
            FileContentBase64 = Convert.ToBase64String(content)
        };

        _context.Files.Add(fileItem);
        await _context.SaveChangesAsync();

        return fileItem;
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        var file = await _context.Files
            .FirstOrDefaultAsync(f => f.Path == filePath);

        if (file == null)
        {
            return false;
        }

        _context.Files.Remove(file);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDirectoryAsync(string directoryPath)
    {
        var directory = await _context.Directories
            .Include(d => d.Files)
            .Include(d => d.Subdirectories)
            .FirstOrDefaultAsync(d => d.Path == directoryPath);

        if (directory == null)
        {
            return false;
        }

        // Delete all files in the directory
        _context.Files.RemoveRange(directory.Files);

        // Recursively delete subdirectories
        foreach (var subdir in directory.Subdirectories)
        {
            await DeleteDirectoryAsync(subdir.Path);
        }

        // Delete the directory itself
        _context.Directories.Remove(directory);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CreateDirectoryAsync(string directoryPath)
    {
        try
        {
            Console.WriteLine($"DatabaseFileService.CreateDirectoryAsync called with path: {directoryPath}");
            
            // Validate input
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                Console.WriteLine("Directory path is null or empty");
                return false;
            }

            // Check if directory already exists
            var existingDir = await _context.Directories
                .FirstOrDefaultAsync(d => d.Path == directoryPath);

            if (existingDir != null)
            {
                Console.WriteLine($"Directory already exists: {directoryPath}");
                return false; // Directory already exists
            }

            // Get parent directory
            DirectoryItem? parentDirectory = null;
            var pathParts = directoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (pathParts.Length > 1)
            {
                var parentPath = string.Join("/", pathParts.Take(pathParts.Length - 1));
                Console.WriteLine($"Looking for parent directory: {parentPath}");
                
                parentDirectory = await _context.Directories
                    .FirstOrDefaultAsync(d => d.Path == parentPath);
                
                if (parentDirectory == null)
                {
                    Console.WriteLine($"Parent directory not found: {parentPath}");
                    // For now, we'll allow creating directories without existing parents
                    // This handles the case where we're creating a root-level directory
                }
            }

            var directoryName = pathParts.LastOrDefault() ?? directoryPath;
            Console.WriteLine($"Creating directory with name: {directoryName}, parentId: {parentDirectory?.Id}");

            var newDirectory = new DirectoryItem
            {
                Name = directoryName,
                Path = directoryPath,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                ParentDirectoryId = parentDirectory?.Id,
                Files = new List<FileItem>(),
                Subdirectories = new List<DirectoryItem>()
            };

            _context.Directories.Add(newDirectory);
            
            Console.WriteLine("Saving directory to database...");
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"Directory created successfully: {directoryPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CreateDirectoryAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> RenameFileAsync(string oldFilePath, string newFileName)
    {
        try
        {
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Path == oldFilePath);
            if (file == null)
            {
                return false;
            }

            // Extract directory path from old file path
            var directoryPath = Path.GetDirectoryName(oldFilePath);
            var newFilePath = string.IsNullOrEmpty(directoryPath) ? newFileName : $"{directoryPath}/{newFileName}";

            // Check if new path already exists
            var existingFile = await _context.Files.FirstOrDefaultAsync(f => f.Path == newFilePath);
            if (existingFile != null)
            {
                return false; // File with new name already exists
            }

            // Update the file record
            file.Path = newFilePath;
            file.Name = newFileName;
            file.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RenameDirectoryAsync(string oldDirectoryPath, string newDirectoryName)
    {
        try
        {
            var directory = await _context.Directories.FirstOrDefaultAsync(d => d.Path == oldDirectoryPath);
            if (directory == null)
            {
                return false;
            }

            // Extract parent directory path
            var parentDirectory = Path.GetDirectoryName(oldDirectoryPath);
            var newDirectoryPath = string.IsNullOrEmpty(parentDirectory) ? newDirectoryName : $"{parentDirectory}/{newDirectoryName}";

            // Check if new path already exists
            var existingDirectory = await _context.Directories.FirstOrDefaultAsync(d => d.Path == newDirectoryPath);
            if (existingDirectory != null)
            {
                return false; // Directory with new name already exists
            }

            // Update all files in this directory and subdirectories
            var filesToUpdate = await _context.Files
                .Where(f => f.Path.StartsWith(oldDirectoryPath + "/") || f.Path == oldDirectoryPath)
                .ToListAsync();

            foreach (var file in filesToUpdate)
            {
                if (file.Path.StartsWith(oldDirectoryPath + "/"))
                {
                    file.Path = file.Path.Replace(oldDirectoryPath + "/", newDirectoryPath + "/");
                }
            }

            // Update all subdirectories
            var subdirectoriesToUpdate = await _context.Directories
                .Where(d => d.Path.StartsWith(oldDirectoryPath + "/"))
                .ToListAsync();

            foreach (var subdir in subdirectoriesToUpdate)
            {
                subdir.Path = subdir.Path.Replace(oldDirectoryPath + "/", newDirectoryPath + "/");
            }

            // Update the main directory
            directory.Path = newDirectoryPath;
            directory.Name = newDirectoryName;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> MoveFileAsync(string sourceFilePath, string destinationDirectoryPath)
    {
        try
        {
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Path == sourceFilePath);
            if (file == null)
            {
                return false;
            }

            DirectoryItem? destinationDirectory = null;
            if (!string.IsNullOrEmpty(destinationDirectoryPath) && destinationDirectoryPath != "/")
            {
                destinationDirectory = await _context.Directories
                    .FirstOrDefaultAsync(d => d.Path == destinationDirectoryPath);
                
                if (destinationDirectory == null)
                {
                    return false; // Destination directory doesn't exist
                }
            }

            // Update file's directory and path
            var fileName = Path.GetFileName(sourceFilePath);
            var newFilePath = string.IsNullOrEmpty(destinationDirectoryPath) || destinationDirectoryPath == "/" 
                ? fileName 
                : $"{destinationDirectoryPath.TrimEnd('/')}/{fileName}";

            // Check if a file already exists at the destination
            var existingFile = await _context.Files.FirstOrDefaultAsync(f => f.Path == newFilePath);
            if (existingFile != null)
            {
                return false; // Don't overwrite existing files
            }

            file.DirectoryId = destinationDirectory?.Id;
            file.Path = newFilePath;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> MoveDirectoryAsync(string sourceDirectoryPath, string destinationDirectoryPath)
    {
        try
        {
            var directory = await _context.Directories.FirstOrDefaultAsync(d => d.Path == sourceDirectoryPath);
            if (directory == null)
            {
                return false;
            }

            DirectoryItem? destinationDirectory = null;
            if (!string.IsNullOrEmpty(destinationDirectoryPath) && destinationDirectoryPath != "/")
            {
                destinationDirectory = await _context.Directories
                    .FirstOrDefaultAsync(d => d.Path == destinationDirectoryPath);
                
                if (destinationDirectory == null)
                {
                    return false; // Destination directory doesn't exist
                }
            }

            // Extract directory name from source path
            var directoryName = Path.GetFileName(sourceDirectoryPath.TrimEnd('/'));
            var newDirectoryPath = string.IsNullOrEmpty(destinationDirectoryPath) || destinationDirectoryPath == "/" 
                ? directoryName 
                : $"{destinationDirectoryPath.TrimEnd('/')}/{directoryName}";

            // Check if a directory already exists at the destination
            var existingDirectory = await _context.Directories.FirstOrDefaultAsync(d => d.Path == newDirectoryPath);
            if (existingDirectory != null)
            {
                return false; // Don't overwrite existing directories
            }

            // Update all files and subdirectories that belong to this directory
            var allFilesToUpdate = await _context.Files
                .Where(f => f.Path.StartsWith(sourceDirectoryPath + "/"))
                .ToListAsync();

            foreach (var file in allFilesToUpdate)
            {
                file.Path = file.Path.Replace(sourceDirectoryPath + "/", newDirectoryPath + "/");
            }

            var allSubdirectoriesToUpdate = await _context.Directories
                .Where(d => d.Path.StartsWith(sourceDirectoryPath + "/"))
                .ToListAsync();

            foreach (var subdir in allSubdirectoriesToUpdate)
            {
                subdir.Path = subdir.Path.Replace(sourceDirectoryPath + "/", newDirectoryPath + "/");
            }

            // Update the main directory
            directory.ParentDirectoryId = destinationDirectory?.Id;
            directory.Path = newDirectoryPath;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
