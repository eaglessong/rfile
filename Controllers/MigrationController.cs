using FileViewer.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileViewer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class MigrationController : ControllerBase
{
    private readonly IMigrationService _migrationService;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(IMigrationService migrationService, ILogger<MigrationController> logger)
    {
        _migrationService = migrationService;
        _logger = logger;
    }

    [HttpPost("migrate-to-blob")]
    public async Task<IActionResult> MigrateToBlob()
    {
        try
        {
            _logger.LogInformation("Migration to blob storage requested by user");
            
            var (migrated, failed) = await _migrationService.MigrateFilesToBlobStorageAsync();
            
            return Ok(new
            {
                Success = true,
                Message = $"Migration completed successfully",
                MigratedFiles = migrated,
                FailedFiles = failed,
                TotalProcessed = migrated + failed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during migration to blob storage");
            return StatusCode(500, new
            {
                Success = false,
                Message = "Error occurred during migration",
                Error = ex.Message
            });
        }
    }

    [HttpPost("validate-migration")]
    public async Task<IActionResult> ValidateMigration()
    {
        try
        {
            _logger.LogInformation("Migration validation requested by user");
            
            var isValid = await _migrationService.ValidateMigrationAsync();
            
            return Ok(new
            {
                Success = true,
                IsValid = isValid,
                Message = isValid ? "All files validated successfully" : "Some files are missing in blob storage"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during migration validation");
            return StatusCode(500, new
            {
                Success = false,
                Message = "Error occurred during validation",
                Error = ex.Message
            });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetMigrationStatus()
    {
        try
        {
            // Simple status check - compare database vs what would be expected
            var isValid = await _migrationService.ValidateMigrationAsync();
            
            return Ok(new
            {
                Success = true,
                MigrationComplete = isValid,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking migration status");
            return StatusCode(500, new
            {
                Success = false,
                Message = "Error occurred while checking status",
                Error = ex.Message
            });
        }
    }
}
