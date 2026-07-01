namespace Flights.Orchestrator.Core;

/// <summary>
/// Resolves well-known paths relative to the repo root, shared by the Console app, the Web API,
/// and EF Core design-time tooling so they all agree on where things live.
/// </summary>
public static class OrchestratorPaths
{
    private const string SolutionFileName = "Flights.sln";

    /// <summary>
    /// Walks up from the given start directory (default: current directory) until it finds
    /// Flights.sln, so tools work the same whether run from the repo root or a project subfolder.
    /// </summary>
    public static string FindRepoRoot(string? startDirectory = null)
    {
        var overridePath = Environment.GetEnvironmentVariable("FLIGHTS_REPO_ROOT");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var dir = new DirectoryInfo(startDirectory ?? Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate '{SolutionFileName}' by walking up from the current directory. " +
            "Set the FLIGHTS_REPO_ROOT environment variable to override.");
    }

    public static string OrchestratorStateDirectory(string? repoRoot = null) =>
        Path.Combine(repoRoot ?? FindRepoRoot(), ".orchestrator");

    public static string OrchestratorDbPath(string? repoRoot = null) =>
        Path.Combine(OrchestratorStateDirectory(repoRoot), "orchestrator.db");

    public static string WorkspaceDirectory(string? repoRoot = null) =>
        Path.Combine(repoRoot ?? FindRepoRoot(), "workspace");

    public static string ProjectBriefPath(string? repoRoot = null) =>
        Path.Combine(repoRoot ?? FindRepoRoot(), "PROJECT_BRIEF.md");
}
