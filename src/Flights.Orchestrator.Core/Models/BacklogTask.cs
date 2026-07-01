namespace Flights.Orchestrator.Core.Models;

public enum BacklogTaskStatus
{
    Pending,
    InProgress,
    Blocked,
    NeedsInput,
    Done
}

/// <summary>
/// A single unit of work produced by the Architect and worked on by the Developer/Reviewer loop.
/// </summary>
public class BacklogTask
{
    public string Id { get; set; } = default!;

    /// <summary>Name of the project this task belongs to. Single project for now ("flights-scraper"),
    /// kept as a column so a second project doesn't require a breaking migration later.</summary>
    public string ProjectName { get; set; } = "flights-scraper";

    public string Description { get; set; } = default!;
    public string AcceptanceCriteria { get; set; } = default!;

    /// <summary>Stored as a JSON array via an EF Core value converter (see OrchestratorDbContext).</summary>
    public List<string> DependsOnTaskIds { get; set; } = new();

    public BacklogTaskStatus Status { get; set; } = BacklogTaskStatus.Pending;

    /// <summary>Current retry count for this task; capped at 3 by the orchestration control flow.</summary>
    public int IterationCount { get; set; }

    public string? LastDeveloperSummaryJson { get; set; }
    public string? LastReviewerVerdictJson { get; set; }

    /// <summary>UTC. Plain DateTime (not DateTimeOffset) because EF Core's SQLite provider cannot
    /// translate ORDER BY on DateTimeOffset columns, even with a value converter.</summary>
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
