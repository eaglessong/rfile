using FileViewer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FileViewer.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<FileItem> Files { get; set; } = null!;
    public DbSet<DirectoryItem> Directories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasConversion<int>();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Create unique indexes
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure FileItem entity
        modelBuilder.Entity<FileItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.Url).HasMaxLength(1000);
            entity.Property(e => e.FileContentBase64).HasColumnType("TEXT");
            
            // Create index on Path for faster lookups
            entity.HasIndex(e => e.Path);
        });

        // Configure DirectoryItem entity
        modelBuilder.Entity<DirectoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            
            // Self-referencing relationship for parent directory
            entity.HasOne<DirectoryItem>()
                  .WithMany()
                  .HasForeignKey(e => e.ParentDirectoryId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Create index on Path for faster lookups
            entity.HasIndex(e => e.Path);
        });

        // Seed default admin user
        var adminPasswordHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.Create()
                .ComputeHash(System.Text.Encoding.UTF8.GetBytes("admin123salt"))
        );
        
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@fileviewer.com",
                PasswordHash = adminPasswordHash,
                Role = UserRole.Owner,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
