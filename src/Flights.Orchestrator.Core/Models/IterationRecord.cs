namespace Flights.Orchestrator.Core.Models;

/// <summary>
/// Audit trail of one Developer -> Reviewer attempt at a <see cref="BacklogTask"/>.
/// </summary>
public class IterationRecord
{
    public int Id { get; set; }
    public string BacklogTaskId { get; set; } = default!;
    public int IterationNumber { get; set; }

    public string DeveloperSummaryJson { get; set; } = default!;
    public string? ReviewerVerdictJson { get; set; }

    /// <summary>UTC. Plain DateTime — see the comment on BacklogTask.CreatedAt for why.</summary>
    public DateTime CreatedAt { get; set; }
}
