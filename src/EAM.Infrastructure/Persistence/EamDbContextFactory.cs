using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace EAM.Infrastructure.Persistence;

public class EamDbContextFactory : IDesignTimeDbContextFactory<EamDbContext>
{
    public EamDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidatePaths = new[]
        {
            Path.Combine(currentDirectory, "src", "EAM.Api"),
            Path.Combine(currentDirectory, "..", "EAM.Api"),
            currentDirectory
        };
        var basePath = candidatePaths
            .Select(Path.GetFullPath)
            .First(Directory.Exists);

        var connectionString = GetConnectionString(basePath)
            ?? "Server=localhost;Database=EamDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<EamDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(EamDbContext).Assembly.FullName))
            .Options;

        return new EamDbContext(options);
    }

    private static string? GetConnectionString(string basePath)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var connectionString = ReadConnectionString(Path.Combine(basePath, "appsettings.json"));
        var developmentConnectionString = ReadConnectionString(Path.Combine(basePath, "appsettings.Development.json"));

        return developmentConnectionString ?? connectionString;
    }

    private static string? ReadConnectionString(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
        {
            return null;
        }

        return connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection)
            ? defaultConnection.GetString()
            : null;
    }
}
