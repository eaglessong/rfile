using FileViewer.Api.Models;
using System.Text.Json;

namespace FileViewer.Api.Services;

public class MockFileData
{
    public List<FileItem> Files { get; set; } = new();
    public List<DirectoryItem> Directories { get; set; } = new();
    public Dictionary<string, string> FileContents { get; set; } = new(); // Base64 encoded content
}

public class MockFileService : IFileService
{
    private static List<FileItem> _mockFiles = new();
    private static List<DirectoryItem> _mockDirectories = new();
    private static readonly Dictionary<string, byte[]> _fileContents = new();
    private static readonly string _dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mock_files_data.json");

    static MockFileService()
    {
        // Try to load existing data from file, otherwise use default sample data
        LoadDataFromFile();
        
        // If no data was loaded, initialize with sample data
        if (_mockFiles.Count == 0)
        {
            InitializeDefaultData();
            SaveDataToFile(); // Save the initial data
        }
    }

    private static void LoadDataFromFile()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var jsonData = File.ReadAllText(_dataFilePath);
                var data = JsonSerializer.Deserialize<MockFileData>(jsonData);
                
                if (data != null)
                {
                    _mockFiles = data.Files ?? new List<FileItem>();
                    _mockDirectories = data.Directories ?? new List<DirectoryItem>();
                    
                    // Load file contents
                    _fileContents.Clear();
                    if (data.FileContents != null)
                    {
                        foreach (var kvp in data.FileContents)
                        {
                            try
                            {
                                _fileContents[kvp.Key] = Convert.FromBase64String(kvp.Value);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error loading file content for {kvp.Key}: {ex.Message}");
                            }
                        }
                    }
                    
                    Console.WriteLine($"Loaded {_mockFiles.Count} files and {_mockDirectories.Count} directories from persistent storage");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading data from file: {ex.Message}");
        }

        // Initialize empty collections if loading failed
        _mockFiles = new List<FileItem>();
        _mockDirectories = new List<DirectoryItem>();
    }

    private static void SaveDataToFile()
    {
        try
        {
            // Convert file contents to Base64 for JSON serialization
            var fileContentsForSerialization = new Dictionary<string, string>();
            foreach (var kvp in _fileContents)
            {
                try
                {
                    fileContentsForSerialization[kvp.Key] = Convert.ToBase64String(kvp.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error encoding file content for {kvp.Key}: {ex.Message}");
                }
            }

            var data = new MockFileData
            {
                Files = _mockFiles,
                Directories = _mockDirectories,
                FileContents = fileContentsForSerialization
            };

            var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(_dataFilePath, jsonData);
            Console.WriteLine($"Saved {_mockFiles.Count} files and {_mockDirectories.Count} directories to persistent storage");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving data to file: {ex.Message}");
        }
    }

    private static void InitializeDefaultData()
    {
        // Start with empty collections - no automatic test files
        // Users will have a clean file system to begin with
        _mockFiles.Clear();
        _mockDirectories.Clear();
        _fileContents.Clear();
        
        Console.WriteLine("Initialized empty file system - no sample files created");
    }

    public async Task<IEnumerable<FileItem>> GetFilesInDirectoryAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            // Return root files
            return await Task.FromResult(_mockFiles);
        }

        // Find directory and return its files
        var directory = _mockDirectories.FirstOrDefault(d => d.Path == path);
        return await Task.FromResult(directory?.Files ?? new List<FileItem>());
    }

    public async Task<IEnumerable<DirectoryItem>> GetDirectoriesAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            // Return root directories
            return await Task.FromResult(_mockDirectories);
        }

        // For now, we don't support nested directories
        return await Task.FromResult(new List<DirectoryItem>());
    }

    public async Task<byte[]> GetFileContentAsync(string filePath)
    {
        // First try to get actual uploaded content
        if (_fileContents.TryGetValue(filePath, out var actualContent))
        {
            return actualContent;
        }

        var file = _mockFiles.FirstOrDefault(f => f.Path == filePath);
        if (file == null)
        {
            // Check in directories
            foreach (var dir in _mockDirectories)
            {
                file = dir.Files?.FirstOrDefault(f => f.Path == filePath);
                if (file != null) break;
            }
        }

        if (file == null)
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        // For mock service, return sample content based on file type
        if (file.ContentType.StartsWith("text/"))
        {
            return System.Text.Encoding.UTF8.GetBytes($"This is sample content for {file.Name}");
        }
        else if (file.ContentType.StartsWith("application/json"))
        {
            return System.Text.Encoding.UTF8.GetBytes($"{{\"message\": \"Sample JSON content for {file.Name}\"}}");
        }
        else
        {
            // Return a small binary placeholder
            return new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // ZIP file header as placeholder
        }
    }

    public async Task<UploadResponse> UploadFileAsync(IFormFile file, string? directoryPath = null)
    {
        var targetPath = directoryPath ?? "";
        
        var fileItem = new FileItem
        {
            Name = file.FileName,
            Path = string.IsNullOrEmpty(targetPath) || targetPath == "/" 
                ? file.FileName 
                : $"{targetPath.TrimEnd('/')}/{file.FileName}",
            Size = file.Length,
            ContentType = file.ContentType,
            LastModified = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            IsDirectory = false,
            Url = "#" // In a real implementation, this would be the blob URL
        };

        // Store file content for later retrieval
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var content = memoryStream.ToArray();
        
        // Store content in dictionary for mock service
        _fileContents[fileItem.Path] = content;

        if (string.IsNullOrEmpty(targetPath) || targetPath == "/")
        {
            // Add to root files
            _mockFiles.Add(fileItem);
        }
        else
        {
            // Add to specific directory
            var directory = _mockDirectories.FirstOrDefault(d => d.Path == targetPath);
            if (directory != null)
            {
                directory.Files ??= new List<FileItem>();
                directory.Files.Add(fileItem);
            }
            else
            {
                // Create directory if it doesn't exist
                var newDirectory = new DirectoryItem
                {
                    Name = targetPath,
                    Path = targetPath,
                    Files = new List<FileItem> { fileItem }
                };
                _mockDirectories.Add(newDirectory);
            }
        }

        // Save the updated data to file
        SaveDataToFile();

        return new UploadResponse
        {
            Success = true,
            Message = "File uploaded successfully",
            FileInfo = fileItem
        };
    }

    public async Task<(byte[] content, string contentType)> GetFileContentWithTypeAsync(string filePath)
    {
        // First try to get actual uploaded content
        if (_fileContents.TryGetValue(filePath, out var actualContent))
        {
            var file = _mockFiles.FirstOrDefault(f => f.Path == filePath);
            if (file == null)
            {
                // Check in directories
                foreach (var dir in _mockDirectories)
                {
                    file = dir.Files?.FirstOrDefault(f => f.Path == filePath);
                    if (file != null) break;
                }
            }

            return (actualContent, file?.ContentType ?? "application/octet-stream");
        }

        // Fallback to original mock content
        var content = await GetFileContentAsync(filePath);
        var fileItem = _mockFiles.FirstOrDefault(f => f.Path == filePath);
        if (fileItem == null)
        {
            // Check in directories
            foreach (var dir in _mockDirectories)
            {
                fileItem = dir.Files?.FirstOrDefault(f => f.Path == filePath);
                if (fileItem != null) break;
            }
        }

        return (content, fileItem?.ContentType ?? "application/octet-stream");
    }

    // Required by interface but not implemented for mock service
    public async Task<List<FileItem>> GetFilesAsync(string directoryPath = "")
    {
        var files = await GetFilesInDirectoryAsync(directoryPath);
        return files.ToList();
    }

    public async Task<DirectoryItem> GetDirectoryStructureAsync(string directoryPath = "")
    {
        var directories = await GetDirectoriesAsync(directoryPath);
        var files = await GetFilesInDirectoryAsync(directoryPath);

        return new DirectoryItem
        {
            Name = string.IsNullOrEmpty(directoryPath) ? "root" : directoryPath,
            Path = directoryPath,
            CreatedDate = DateTime.UtcNow.AddDays(-15),
            LastModified = DateTime.UtcNow.AddDays(-1),
            Files = files.ToList(),
            Subdirectories = directories.ToList()
        };
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        // Remove from file contents
        _fileContents.Remove(filePath);

        // Remove from mock files
        var file = _mockFiles.FirstOrDefault(f => f.Path == filePath);
        if (file != null)
        {
            _mockFiles.Remove(file);
            SaveDataToFile();
            return true;
        }

        // Remove from directories
        foreach (var dir in _mockDirectories)
        {
            var fileInDir = dir.Files?.FirstOrDefault(f => f.Path == filePath);
            if (fileInDir != null)
            {
                dir.Files?.Remove(fileInDir);
                SaveDataToFile();
                return true;
            }
        }

        return false;
    }

    public async Task<bool> DeleteDirectoryAsync(string directoryPath)
    {
        // Find the directory to delete
        var directoryToDelete = _mockDirectories.FirstOrDefault(d => d.Path == directoryPath);
        if (directoryToDelete == null)
        {
            return false; // Directory not found
        }

        // Remove all files in the directory from file contents
        if (directoryToDelete.Files != null)
        {
            foreach (var file in directoryToDelete.Files)
            {
                _fileContents.Remove(file.Path);
            }
        }

        // Remove all files from mock files that belong to this directory or subdirectories
        var filesToRemove = _mockFiles.Where(f => f.Path.StartsWith(directoryPath + "/")).ToList();
        foreach (var file in filesToRemove)
        {
            _mockFiles.Remove(file);
            _fileContents.Remove(file.Path);
        }

        // Remove subdirectories recursively
        var subdirectoriesToRemove = _mockDirectories.Where(d => d.Path.StartsWith(directoryPath + "/")).ToList();
        foreach (var subdir in subdirectoriesToRemove)
        {
            _mockDirectories.Remove(subdir);
        }

        // Remove the directory itself
        _mockDirectories.Remove(directoryToDelete);
        
        SaveDataToFile();
        return true;
    }

    public async Task<string> GetDownloadUrlAsync(string filePath)
    {
        // For mock service, return a placeholder URL
        return await Task.FromResult($"/api/files/download/{Uri.EscapeDataString(filePath)}");
    }

    public async Task<bool> CreateDirectoryAsync(string directoryPath)
    {
        var existingDir = _mockDirectories.FirstOrDefault(d => d.Path == directoryPath);
        if (existingDir != null)
        {
            return false; // Directory already exists
        }

        var newDirectory = new DirectoryItem
        {
            Name = directoryPath.Split('/').Last(),
            Path = directoryPath,
            CreatedDate = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            Files = new List<FileItem>(),
            Subdirectories = new List<DirectoryItem>()
        };

        _mockDirectories.Add(newDirectory);
        SaveDataToFile();
        return true;
    }

    public async Task<bool> RenameFileAsync(string oldFilePath, string newFileName)
    {
        var file = _mockFiles.FirstOrDefault(f => f.Path == oldFilePath);
        if (file == null)
        {
            return false;
        }

        // Extract directory path from old file path
        var directoryPath = Path.GetDirectoryName(oldFilePath);
        var newFilePath = string.IsNullOrEmpty(directoryPath) ? newFileName : $"{directoryPath}/{newFileName}";

        // Check if new path already exists
        if (_mockFiles.Any(f => f.Path == newFilePath))
        {
            return false;
        }

        // Update file content mapping
        if (_fileContents.ContainsKey(oldFilePath))
        {
            var content = _fileContents[oldFilePath];
            _fileContents.Remove(oldFilePath);
            _fileContents[newFilePath] = content;
        }

        // Update the file record
        file.Path = newFilePath;
        file.Name = newFileName;
        file.LastModified = DateTime.UtcNow;

        SaveDataToFile();
        return true;
    }

    public async Task<bool> RenameDirectoryAsync(string oldDirectoryPath, string newDirectoryName)
    {
        var directory = _mockDirectories.FirstOrDefault(d => d.Path == oldDirectoryPath);
        if (directory == null)
        {
            return false;
        }

        // Extract parent directory path
        var parentDirectory = Path.GetDirectoryName(oldDirectoryPath);
        var newDirectoryPath = string.IsNullOrEmpty(parentDirectory) ? newDirectoryName : $"{parentDirectory}/{newDirectoryName}";

        // Check if new path already exists
        if (_mockDirectories.Any(d => d.Path == newDirectoryPath))
        {
            return false;
        }

        // Update all files in this directory and subdirectories
        var filesToUpdate = _mockFiles.Where(f => f.Path.StartsWith(oldDirectoryPath + "/")).ToList();
        foreach (var file in filesToUpdate)
        {
            var oldPath = file.Path;
            file.Path = file.Path.Replace(oldDirectoryPath + "/", newDirectoryPath + "/");
            
            // Update file content mapping
            if (_fileContents.ContainsKey(oldPath))
            {
                var content = _fileContents[oldPath];
                _fileContents.Remove(oldPath);
                _fileContents[file.Path] = content;
            }
        }

        // Update all subdirectories
        var subdirectoriesToUpdate = _mockDirectories.Where(d => d.Path.StartsWith(oldDirectoryPath + "/")).ToList();
        foreach (var subdir in subdirectoriesToUpdate)
        {
            subdir.Path = subdir.Path.Replace(oldDirectoryPath + "/", newDirectoryPath + "/");
        }

        // Update the main directory
        directory.Path = newDirectoryPath;
        directory.Name = newDirectoryName;

        SaveDataToFile();
        return true;
    }

    public async Task<bool> MoveFileAsync(string sourceFilePath, string destinationDirectoryPath)
    {
        // For mock service, just simulate the move operation
        var file = _mockFiles.FirstOrDefault(f => f.Path == sourceFilePath);
        if (file == null)
            throw new FileNotFoundException($"File '{sourceFilePath}' not found");

        // Calculate the new path
        var fileName = Path.GetFileName(sourceFilePath);
        var newPath = string.IsNullOrEmpty(destinationDirectoryPath) 
            ? fileName 
            : $"{destinationDirectoryPath.TrimEnd('/')}/{fileName}";

        // Update file path
        var oldPath = file.Path;
        file.Path = newPath;

        // Update file content mapping if it exists
        if (_fileContents.ContainsKey(oldPath))
        {
            var content = _fileContents[oldPath];
            _fileContents.Remove(oldPath);
            _fileContents[newPath] = content;
        }

        SaveDataToFile();
        return true;
    }

    public async Task<bool> MoveDirectoryAsync(string sourceDirectoryPath, string destinationDirectoryPath)
    {
        // For mock service, just simulate the move operation
        var directory = _mockDirectories.FirstOrDefault(d => d.Path == sourceDirectoryPath);
        if (directory == null)
            throw new DirectoryNotFoundException($"Directory '{sourceDirectoryPath}' not found");

        // Calculate the new path
        var directoryName = Path.GetFileName(sourceDirectoryPath);
        var newPath = string.IsNullOrEmpty(destinationDirectoryPath) 
            ? directoryName 
            : $"{destinationDirectoryPath.TrimEnd('/')}/{directoryName}";

        // Update all files in this directory
        var filesToUpdate = _mockFiles.Where(f => f.Path.StartsWith(sourceDirectoryPath + "/") || f.Path == sourceDirectoryPath).ToList();
        foreach (var file in filesToUpdate)
        {
            var oldPath = file.Path;
            file.Path = file.Path.Replace(sourceDirectoryPath, newPath);
            
            // Update file content mapping
            if (_fileContents.ContainsKey(oldPath))
            {
                var content = _fileContents[oldPath];
                _fileContents.Remove(oldPath);
                _fileContents[file.Path] = content;
            }
        }

        // Update all subdirectories
        var subdirectoriesToUpdate = _mockDirectories.Where(d => d.Path.StartsWith(sourceDirectoryPath + "/") || d.Path == sourceDirectoryPath).ToList();
        foreach (var subdir in subdirectoriesToUpdate)
        {
            subdir.Path = subdir.Path.Replace(sourceDirectoryPath, newPath);
        }

        SaveDataToFile();
        return true;
    }
}