using Azure.Storage.Blobs;
using FileViewer.Api.Data;
using FileViewer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FileViewer.Api.Services;

public interface IMigrationService
{
    Task<(int migrated, int failed)> MigrateFilesToBlobStorageAsync();
    Task<bool> ValidateMigrationAsync();
}

public class MigrationService : IMigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<MigrationService> _logger;
    private const string ContainerName = "files";

    public MigrationService(
        ApplicationDbContext context, 
        BlobServiceClient blobServiceClient,
        ILogger<MigrationService> logger)
    {
        _context = context;
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<(int migrated, int failed)> MigrateFilesToBlobStorageAsync()
    {
        _logger.LogInformation("Starting migration from database to blob storage...");
        
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync();

        var files = await _context.Files
            .Where(f => !string.IsNullOrEmpty(f.FileContentBase64))
            .ToListAsync();

        int migrated = 0;
        int failed = 0;

        foreach (var file in files)
        {
            try
            {
                _logger.LogInformation($"Migrating file: {file.Path}");

                // Decode base64 content
                var content = Convert.FromBase64String(file.FileContentBase64);
                
                // Upload to blob storage
                var blobClient = containerClient.GetBlobClient(file.Path);
                using var stream = new MemoryStream(content);
                
                await blobClient.UploadAsync(stream, overwrite: true);
                
                // Set blob properties
                await blobClient.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });

                // Clear the base64 content from database to save space
                file.FileContentBase64 = string.Empty;
                
                migrated++;
                _logger.LogInformation($"Successfully migrated: {file.Path}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to migrate file: {file.Path}");
                failed++;
            }
        }

        // Save changes to clear FileContentBase64 fields
        if (migrated > 0)
        {
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation($"Migration completed. Migrated: {migrated}, Failed: {failed}");
        return (migrated, failed);
    }

    public async Task<bool> ValidateMigrationAsync()
    {
        _logger.LogInformation("Validating migration...");
        
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        
        var dbFiles = await _context.Files.ToListAsync();
        int validatedCount = 0;
        int missingCount = 0;

        foreach (var file in dbFiles)
        {
            try
            {
                var blobClient = containerClient.GetBlobClient(file.Path);
                var exists = await blobClient.ExistsAsync();
                
                if (exists)
                {
                    validatedCount++;
                }
                else
                {
                    _logger.LogWarning($"File missing in blob storage: {file.Path}");
                    missingCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating file: {file.Path}");
                missingCount++;
            }
        }

        _logger.LogInformation($"Validation completed. Validated: {validatedCount}, Missing: {missingCount}");
        return missingCount == 0;
    }
}
