using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NexaStore.Infrastructure.Persistence;

public sealed class NexaStoreDbContextFactory
    : IDesignTimeDbContextFactory<NexaStoreDbContext>
{
    public NexaStoreDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var possiblePaths = new[]
        {
            Path.Combine(currentDirectory, "appsettings.json"),

            Path.Combine(
                currentDirectory,
                "NexaStore.API",
                "appsettings.json"
            ),

            Path.Combine(
                currentDirectory,
                "..",
                "NexaStore.API",
                "appsettings.json"
            )
        };

        var settingsPath = possiblePaths
            .Select(Path.GetFullPath)
            .FirstOrDefault(File.Exists);

        if (settingsPath is null)
        {
            throw new FileNotFoundException(
                $"appsettings.json not found. Current directory: {currentDirectory}"
            );
        }

        using var document = JsonDocument.Parse(
            File.ReadAllText(settingsPath)
        );

        var connectionString = document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string was not found."
            );
        }

        var options = new DbContextOptionsBuilder<NexaStoreDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new NexaStoreDbContext(options);
    }
}