using System.Text.Json;
using Flights.Orchestrator.Core.Agents;
using Flights.Orchestrator.Core.Models;
using Flights.Orchestrator.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Flights.Orchestrator.Core.Orchestration;

public enum RunOutcome
{
    NeedsInput,
    BacklogCreated,
    TaskCompleted,
    TaskBlocked,
    ProjectComplete,
    NoRunnableTask,
}

public record RunResult(RunOutcome Outcome, string Message);

/// <summary>
/// Drives one "task-cycle" of the Architect/Developer/Reviewer loop against the shared SQLite
/// state store. See PROJECT PLAN section "Kontrolni tok" for the full pseudocode this implements.
/// </summary>
public class OrchestratorRunner(
    OrchestratorDbContext db,
    ArchitectAgent architect,
    DeveloperAgent developer,
    ReviewerAgent reviewer,
    string projectBriefPath)
{
    private const int MaxIterationsPerTask = 3;

    /// <summary>Advances the project by exactly one task-cycle (resume pass, then either create the
    /// backlog or work one runnable task to a conclusion).</summary>
    public async Task<RunResult> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        await ResumeAnsweredQuestionsAsync(cancellationToken);

        var hasBacklog = await db.BacklogTasks.AnyAsync(cancellationToken);
        var openArchitectQuestion = await db.PendingQuestions.AnyAsync(
            q => q.Source == PendingQuestionSource.Architect && q.Status == PendingQuestionStatus.Open,
            cancellationToken);

        if (!hasBacklog && openArchitectQuestion)
        {
            return new RunResult(RunOutcome.NeedsInput,
                "Waiting on an answer to the Architect's question before a backlog can be created.");
        }

        if (!hasBacklog)
        {
            return await RunArchitectAsync(priorQuestionAndAnswer: null, cancellationToken);
        }

        var task = await PickNextRunnableTaskAsync(cancellationToken);
        if (task is null)
        {
            var anyNeedsHuman = await db.BacklogTasks.AnyAsync(
                t => t.Status == BacklogTaskStatus.Blocked || t.Status == BacklogTaskStatus.NeedsInput,
                cancellationToken);
            if (anyNeedsHuman)
            {
                return new RunResult(RunOutcome.NeedsInput,
                    "One or more tasks are blocked or need input; waiting on a human answer.");
            }

            var allDone = await db.BacklogTasks.AllAsync(t => t.Status == BacklogTaskStatus.Done, cancellationToken);
            return allDone
                ? new RunResult(RunOutcome.ProjectComplete, "All backlog tasks are done.")
                : new RunResult(RunOutcome.NoRunnableTask, "No task is currently runnable (unmet dependencies).");
        }

        return await RunDeveloperReviewerLoopAsync(task, cancellationToken);
    }

    /// <summary>Keeps calling RunOnceAsync, printing a summary each time, until nothing is runnable,
    /// a human answer is needed, or the project is complete.</summary>
    public async Task RunUntilBlockedOrDoneAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var result = await RunOnceAsync(cancellationToken);
            Console.WriteLine($"[{result.Outcome}] {result.Message}");

            if (result.Outcome is RunOutcome.NeedsInput or RunOutcome.ProjectComplete or RunOutcome.NoRunnableTask)
            {
                break;
            }
        }
    }

    private async Task ResumeAnsweredQuestionsAsync(CancellationToken cancellationToken)
    {
        var answered = await db.PendingQuestions
            .Where(q => q.Status == PendingQuestionStatus.Answered && !q.Consumed)
            .ToListAsync(cancellationToken);

        foreach (var question in answered)
        {
            if (question.Source == PendingQuestionSource.Architect)
            {
                var qa = $"Q: {question.QuestionText}\nA: {question.AnswerText}";
                await RunArchitectAsync(priorQuestionAndAnswer: qa, cancellationToken);
            }
            else if (question.Source == PendingQuestionSource.BlockedTaskEscalation && question.BacklogTaskId is not null)
            {
                var task = await db.BacklogTasks.FindAsync([question.BacklogTaskId], cancellationToken);
                if (task is not null)
                {
                    task.IterationCount = 0;
                    task.Status = BacklogTaskStatus.Pending;
                    task.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            question.Consumed = true;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<RunResult> RunArchitectAsync(string? priorQuestionAndAnswer, CancellationToken cancellationToken)
    {
        var brief = await File.ReadAllTextAsync(projectBriefPath, cancellationToken);
        var result = await architect.RunAsync(brief, priorQuestionAndAnswer, cancellationToken);

        if (result.IsNeedsInput)
        {
            db.PendingQuestions.Add(new PendingQuestion
            {
                Id = Guid.NewGuid().ToString(),
                Source = PendingQuestionSource.Architect,
                QuestionText = result.NeedsInputQuestion!,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
            return new RunResult(RunOutcome.NeedsInput, $"Architect needs input: {result.NeedsInputQuestion}");
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var t in result.Tasks)
        {
            db.BacklogTasks.Add(new BacklogTask
            {
                Id = t.Id,
                Description = t.Description,
                AcceptanceCriteria = t.AcceptanceCriteria,
                DependsOnTaskIds = t.DependsOn,
                Status = BacklogTaskStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return new RunResult(RunOutcome.BacklogCreated, $"Architect created a backlog of {result.Tasks.Count} task(s).");
    }

    private async Task<BacklogTask?> PickNextRunnableTaskAsync(CancellationToken cancellationToken)
    {
        var candidates = await db.BacklogTasks
            .Where(t => t.Status == BacklogTaskStatus.Pending || t.Status == BacklogTaskStatus.InProgress)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var doneIds = await db.BacklogTasks
            .Where(t => t.Status == BacklogTaskStatus.Done)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
        var doneSet = doneIds.ToHashSet();

        return candidates.FirstOrDefault(t => t.DependsOnTaskIds.All(doneSet.Contains));
    }

    private async Task<RunResult> RunDeveloperReviewerLoopAsync(BacklogTask task, CancellationToken cancellationToken)
    {
        task.Status = BacklogTaskStatus.InProgress;
        await db.SaveChangesAsync(cancellationToken);

        var feedback = task.LastReviewerVerdictJson is not null
            ? SummarizeFindingsForFeedback(task.LastReviewerVerdictJson)
            : null;

        while (true)
        {
            var iterationNumber = task.IterationCount + 1;
            if (iterationNumber > MaxIterationsPerTask)
            {
                task.Status = BacklogTaskStatus.Blocked;
                task.UpdatedAt = DateTimeOffset.UtcNow;

                db.PendingQuestions.Add(new PendingQuestion
                {
                    Id = Guid.NewGuid().ToString(),
                    BacklogTaskId = task.Id,
                    Source = PendingQuestionSource.BlockedTaskEscalation,
                    QuestionText = $"Task {task.Id} failed review {MaxIterationsPerTask} times. " +
                                   $"Last findings: {task.LastReviewerVerdictJson}. How should I proceed?",
                    CreatedAt = DateTimeOffset.UtcNow,
                });

                await db.SaveChangesAsync(cancellationToken);
                return new RunResult(RunOutcome.TaskBlocked,
                    $"Task {task.Id} blocked after {MaxIterationsPerTask} failed review iterations.");
            }

            var developerResult = await developer.RunAsync(
                task.Description, task.AcceptanceCriteria, feedback, cancellationToken);
            var developerSummaryJson = JsonSerializer.Serialize(developerResult);

            var reviewerResult = await reviewer.RunAsync(
                task.Description, task.AcceptanceCriteria, developerSummaryJson, cancellationToken);
            var reviewerVerdictJson = JsonSerializer.Serialize(reviewerResult);

            db.IterationRecords.Add(new IterationRecord
            {
                BacklogTaskId = task.Id,
                IterationNumber = iterationNumber,
                DeveloperSummaryJson = developerSummaryJson,
                ReviewerVerdictJson = reviewerVerdictJson,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            task.IterationCount = iterationNumber;
            task.LastDeveloperSummaryJson = developerSummaryJson;
            task.LastReviewerVerdictJson = reviewerVerdictJson;
            task.UpdatedAt = DateTimeOffset.UtcNow;

            if (reviewerResult.Verdict == ReviewerVerdict.Approve)
            {
                task.Status = BacklogTaskStatus.Done;
                await db.SaveChangesAsync(cancellationToken);
                return new RunResult(RunOutcome.TaskCompleted,
                    $"Task {task.Id} approved after {iterationNumber} iteration(s).");
            }

            feedback = SummarizeFindingsForFeedback(reviewerVerdictJson);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static string SummarizeFindingsForFeedback(string reviewerVerdictJson)
    {
        var verdict = JsonSerializer.Deserialize<ReviewerResult>(reviewerVerdictJson)!;
        return verdict.Findings.Count == 0
            ? "(no specific findings recorded)"
            : string.Join("\n", verdict.Findings.Select(f => $"- {f.File}: {f.Issue} (suggested fix: {f.SuggestedFix})"));
    }
}
