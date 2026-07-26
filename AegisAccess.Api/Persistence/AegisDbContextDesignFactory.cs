using AegisAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AegisAccess.Api.Persistence;

public sealed class AegisDbContextDesignFactory : IDesignTimeDbContextFactory<AegisDbContext>
{
    public AegisDbContext CreateDbContext(string[] args)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "AegisAccess.sln")))
        {
            directory = directory.Parent;
        }

        var projectRoot = directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the AegisAccess solution root.");

        var dataDirectory = Path.Combine(projectRoot, "data");
        Directory.CreateDirectory(dataDirectory);
        var databasePath = Path.Combine(dataDirectory, "aegisaccess.dev.db");

        var options = new DbContextOptionsBuilder<AegisDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new AegisDbContext(options);
    }
}
