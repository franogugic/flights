using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Flights.Orchestrator.Core.Persistence;

/// <summary>
/// Lets EF Core tooling (`dotnet ef migrations add`) create a DbContext at design time,
/// without needing the Console or Api project's DI container wired up.
/// </summary>
public class OrchestratorDbContextFactory : IDesignTimeDbContextFactory<OrchestratorDbContext>
{
    public OrchestratorDbContext CreateDbContext(string[] args)
    {
        var dbPath = OrchestratorPaths.OrchestratorDbPath();
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var optionsBuilder = new DbContextOptionsBuilder<OrchestratorDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new OrchestratorDbContext(optionsBuilder.Options);
    }
}
