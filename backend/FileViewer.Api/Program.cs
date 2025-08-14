using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using FileViewer.Api.Data;
using FileViewer.Api.Models;
using FileViewer.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Azure services
var azureConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration["AZURE_STORAGE_CONNECTION_STRING"];

if (!string.IsNullOrEmpty(azureConnectionString) && azureConnectionString != "UseDevelopmentStorage=true")
{
    builder.Services.AddSingleton(new BlobServiceClient(azureConnectionString));
    builder.Services.AddScoped<IFileService, FileService>();
}
else
{
    // Use database service for persistent storage across deployments
    builder.Services.AddScoped<IFileService, DatabaseFileService>();
}

// Add Key Vault if available
var keyVaultUri = builder.Configuration["KEY_VAULT_URI"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    var credential = new DefaultAzureCredential();
    builder.Services.AddSingleton(new SecretClient(new Uri(keyVaultUri), credential));
}

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
});

// Add Database
var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(dbConnectionString))
{
    // For Azure App Service, use a writable directory
    var dataDirectory = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("TEMP") ?? "/tmp";
    var dbPath = Path.Combine(dataDirectory, "users.db");
    dbConnectionString = $"Data Source={dbPath}";
    Console.WriteLine($"Using database path: {dbPath}");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(dbConnectionString));

// Add custom services
builder.Services.AddScoped<IUserService, UserService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add JWT Authentication
var jwtKey = builder.Configuration["JWT_SECRET_KEY"] ?? "super-secret-key-for-development-only-change-in-production";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false, // Configure in production
        ValidateAudience = false, // Configure in production
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Initialize Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    try
    {
        context.Database.EnsureCreated();
        
        // Seed default Owner account if no users exist
        if (!context.Users.Any())
        {
            var defaultOwner = new User
            {
                Username = "owner",
                Email = "owner@example.com",
                PasswordHash = HashPassword("owner123"),
                Role = UserRole.Owner,
                CreatedAt = DateTime.UtcNow
            };
            
            context.Users.Add(defaultOwner);
            await context.SaveChangesAsync();
            
            Console.WriteLine("✅ Created default owner account - Username: owner, Password: owner123");
        }
        else
        {
            Console.WriteLine($"ℹ️ Database already has {context.Users.Count()} users, skipping seeding");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error during database initialization: {ex.Message}");
    }
}

// Password hashing helper method (matching UserService implementation)
static string HashPassword(string password)
{
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
    return Convert.ToBase64String(hashedBytes);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
