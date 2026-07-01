namespace Flights.Orchestrator.Core.Models;

public enum PendingQuestionStatus
{
    Open,
    Answered
}

public enum PendingQuestionSource
{
    Architect,
    BlockedTaskEscalation
}

/// <summary>
/// A question raised by the Architect (before a backlog exists) or by the control flow when a
/// task exhausts its iteration budget. Answered via the escalation web form.
/// </summary>
public class PendingQuestion
{
    public string Id { get; set; } = default!;
    public string ProjectName { get; set; } = "flights-scraper";

    /// <summary>Null when raised by the Architect before any backlog task exists.</summary>
    public string? BacklogTaskId { get; set; }

    public PendingQuestionSource Source { get; set; }
    public string QuestionText { get; set; } = default!;

    /// <summary>Optional extra context (e.g. reviewer findings) serialized as JSON, shown to the human answering.</summary>
    public string? ContextJson { get; set; }

    public PendingQuestionStatus Status { get; set; } = PendingQuestionStatus.Open;
    public string? AnswerText { get; set; }

    /// <summary>Set once the resume pass has acted on the answer, so it isn't reprocessed on the next run.</summary>
    public bool Consumed { get; set; }

    /// <summary>UTC. Plain DateTime — see the comment on BacklogTask.CreatedAt for why.</summary>
    public DateTime CreatedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
}
