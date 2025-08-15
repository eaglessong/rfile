using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using FileViewer.Api.Models;

namespace FileViewer.Api.Services;

public interface IFileService
{
    Task<List<FileItem>> GetFilesAsync(string directoryPath = "");
    Task<DirectoryItem> GetDirectoryStructureAsync(string directoryPath = "");
    Task<UploadResponse> UploadFileAsync(IFormFile file, string? directoryPath = null);
    Task<bool> DeleteFileAsync(string filePath);
    Task<bool> DeleteDirectoryAsync(string directoryPath);
    Task<string> GetDownloadUrlAsync(string filePath);
    Task<bool> CreateDirectoryAsync(string directoryPath);
    Task<(byte[] content, string contentType)> GetFileContentWithTypeAsync(string filePath);
}

public class FileService : IFileService
{
    private readonly BlobServiceClient _blobServiceClient;
    private const string ContainerName = "files";

    public FileService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<List<FileItem>> GetFilesAsync(string directoryPath = "")
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var files = new List<FileItem>();
        var prefix = string.IsNullOrEmpty(directoryPath) ? "" : $"{directoryPath.TrimEnd('/')}/";

        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
        {
            // Skip directory markers and nested files for this level
            var relativePath = blobItem.Name.Substring(prefix.Length);
            if (relativePath.Contains('/'))
                continue;

            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            var properties = await blobClient.GetPropertiesAsync();

            files.Add(new FileItem
            {
                Name = Path.GetFileName(blobItem.Name),
                Path = blobItem.Name,
                Size = blobItem.Properties.ContentLength ?? 0,
                ContentType = properties.Value.ContentType,
                LastModified = blobItem.Properties.LastModified?.DateTime ?? DateTime.MinValue,
                CreatedDate = properties.Value.CreatedOn.DateTime,
                IsDirectory = false,
                Url = blobClient.Uri.ToString()
            });
        }

        return files;
    }

    public async Task<DirectoryItem> GetDirectoryStructureAsync(string directoryPath = "")
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var prefix = string.IsNullOrEmpty(directoryPath) ? "" : $"{directoryPath.TrimEnd('/')}/";
        var directories = new Dictionary<string, DirectoryItem>();
        var rootDirectory = new DirectoryItem
        {
            Name = string.IsNullOrEmpty(directoryPath) ? "Root" : Path.GetFileName(directoryPath),
            Path = directoryPath,
            CreatedDate = DateTime.UtcNow.AddDays(-30), // Default for root directory
            LastModified = DateTime.UtcNow
        };

        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
        {
            var relativePath = blobItem.Name.Substring(prefix.Length);
            var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 1)
            {
                // This is a file in the current directory
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var properties = await blobClient.GetPropertiesAsync();

                rootDirectory.Files.Add(new FileItem
                {
                    Name = segments[0],
                    Path = blobItem.Name,
                    Size = blobItem.Properties.ContentLength ?? 0,
                    ContentType = properties.Value.ContentType,
                    LastModified = blobItem.Properties.LastModified?.DateTime ?? DateTime.MinValue,
                    CreatedDate = properties.Value.CreatedOn.DateTime,
                    IsDirectory = false,
                    Url = blobClient.Uri.ToString()
                });
            }
            else if (segments.Length > 1)
            {
                // This is a file in a subdirectory
                var subdirName = segments[0];
                var subdirPath = string.IsNullOrEmpty(directoryPath) ? subdirName : $"{directoryPath}/{subdirName}";

                if (!directories.ContainsKey(subdirName))
                {
                    directories[subdirName] = new DirectoryItem
                    {
                        Name = subdirName,
                        Path = subdirPath,
                        CreatedDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)), // Random creation time for demo
                        LastModified = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 7)),
                        Files = new List<FileItem>(),
                        Subdirectories = new List<DirectoryItem>()
                    };
                }

                // Add this file to the subdirectory's file list for accurate counting
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var properties = await blobClient.GetPropertiesAsync();

                directories[subdirName].Files.Add(new FileItem
                {
                    Name = segments[segments.Length - 1], // Get the actual filename
                    Path = blobItem.Name,
                    Size = blobItem.Properties.ContentLength ?? 0,
                    ContentType = properties.Value.ContentType,
                    LastModified = blobItem.Properties.LastModified?.DateTime ?? DateTime.MinValue,
                    CreatedDate = properties.Value.CreatedOn.DateTime,
                    IsDirectory = false,
                    Url = blobClient.Uri.ToString()
                });
            }
        }

        rootDirectory.Subdirectories.AddRange(directories.Values);
        return rootDirectory;
    }

    public async Task<UploadResponse> UploadFileAsync(IFormFile file, string? directoryPath = null)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var fileName = file.FileName;
            var blobPath = string.IsNullOrEmpty(directoryPath) 
                ? fileName 
                : $"{directoryPath.TrimEnd('/')}/{fileName}";

            var blobClient = containerClient.GetBlobClient(blobPath);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);
            
            await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders
            {
                ContentType = file.ContentType
            });

            var properties = await blobClient.GetPropertiesAsync();

            return new UploadResponse
            {
                Success = true,
                Message = "File uploaded successfully",
                FileInfo = new FileItem
                {
                    Name = fileName,
                    Path = blobPath,
                    Size = file.Length,
                    ContentType = file.ContentType,
                    LastModified = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    IsDirectory = false,
                    Url = blobClient.Uri.ToString()
                }
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

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = containerClient.GetBlobClient(filePath);
            
            var response = await blobClient.DeleteIfExistsAsync();
            return response.Value;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteDirectoryAsync(string directoryPath)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var prefix = $"{directoryPath.TrimEnd('/')}/";
            
            var blobsToDelete = new List<string>();
            
            // Find all blobs with the directory prefix
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                blobsToDelete.Add(blobItem.Name);
            }
            
            // Also check for the placeholder file that might represent the directory itself
            var placeholderPath = $"{directoryPath.TrimEnd('/')}/.placeholder";
            var placeholderBlob = containerClient.GetBlobClient(placeholderPath);
            if (await placeholderBlob.ExistsAsync())
            {
                blobsToDelete.Add(placeholderPath);
            }
            
            // Delete all found blobs
            foreach (var blobName in blobsToDelete)
            {
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();
            }
            
            return blobsToDelete.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public Task<string> GetDownloadUrlAsync(string filePath)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(filePath);

        if (blobClient.CanGenerateSasUri)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = ContainerName,
                BlobName = filePath,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return Task.FromResult(blobClient.GenerateSasUri(sasBuilder).ToString());
        }

        return Task.FromResult(blobClient.Uri.ToString());
    }

    public async Task<bool> CreateDirectoryAsync(string directoryPath)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Create a placeholder file to represent the directory
            var placeholderPath = $"{directoryPath.TrimEnd('/')}/.placeholder";
            var blobClient = containerClient.GetBlobClient(placeholderPath);

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("This is a placeholder file to create a directory structure."));
            await blobClient.UploadAsync(stream, overwrite: true);

            return true;
        }
        catch
        {
            return false;
        }
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

            var response = await blobClient.DownloadContentAsync();
            var content = response.Value.Content.ToArray();
            var contentType = response.Value.Details.ContentType ?? "application/octet-stream";

            return (content, contentType);
        }
        catch
        {
            return (Array.Empty<byte>(), "application/octet-stream");
        }
    }
}
