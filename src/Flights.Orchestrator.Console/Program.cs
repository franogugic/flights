using Anthropic;
using Anthropic.Exceptions;
using Flights.Orchestrator.Core;
using Flights.Orchestrator.Core.Agents;
using Flights.Orchestrator.Core.Orchestration;
using Flights.Orchestrator.Core.Persistence;
using Flights.Orchestrator.Core.Tools;
using Microsoft.EntityFrameworkCore;

var runUntilBlockedOrDone = args.Contains("--run-until-blocked-or-done");

var repoRoot = OrchestratorPaths.FindRepoRoot();
var dbPath = OrchestratorPaths.OrchestratorDbPath(repoRoot);
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

var workspaceDirectory = OrchestratorPaths.WorkspaceDirectory(repoRoot);
Directory.CreateDirectory(workspaceDirectory);

var projectBriefPath = OrchestratorPaths.ProjectBriefPath(repoRoot);
if (!File.Exists(projectBriefPath))
{
    Console.Error.WriteLine($"PROJECT_BRIEF.md not found at '{projectBriefPath}'.");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<OrchestratorDbContext>();
optionsBuilder.UseSqlite($"Data Source={dbPath}");

await using var db = new OrchestratorDbContext(optionsBuilder.Options);
await db.Database.MigrateAsync();

var client = new AnthropicClient();

var repoRootSandbox = new PathSandbox(repoRoot);
var workspaceSandbox = new PathSandbox(workspaceDirectory);

var architect = new ArchitectAgent(client, repoRootSandbox);
var developer = new DeveloperAgent(client, workspaceSandbox);
var reviewer = new ReviewerAgent(client, workspaceSandbox);

var runner = new OrchestratorRunner(db, architect, developer, reviewer, projectBriefPath);

try
{
    if (runUntilBlockedOrDone)
    {
        await runner.RunUntilBlockedOrDoneAsync();
    }
    else
    {
        var result = await runner.RunOnceAsync();
        Console.WriteLine($"[{result.Outcome}] {result.Message}");
    }
}
catch (AnthropicUnauthorizedException)
{
    Console.Error.WriteLine(
        "Anthropic API authentication failed. Set the ANTHROPIC_API_KEY environment variable " +
        "(or run 'ant auth login' if the Anthropic CLI is installed) and try again.");
    return 1;
}
catch (AnthropicApiException ex)
{
    Console.Error.WriteLine($"Anthropic API error: {ex.Message}");
    return 1;
}

return 0;
