using Flights.Orchestrator.Core.Models;

namespace Flights.Orchestrator.Api.Dtos;

public record BacklogTaskDto(
    string Id,
    string ProjectName,
    string Description,
    string AcceptanceCriteria,
    List<string> DependsOnTaskIds,
    string Status,
    int IterationCount,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static BacklogTaskDto FromEntity(BacklogTask t) => new(
        t.Id,
        t.ProjectName,
        t.Description,
        t.AcceptanceCriteria,
        t.DependsOnTaskIds,
        t.Status.ToString(),
        t.IterationCount,
        t.CreatedAt,
        t.UpdatedAt);
}

public record IterationRecordDto(
    int IterationNumber,
    string DeveloperSummaryJson,
    string? ReviewerVerdictJson,
    DateTime CreatedAt)
{
    public static IterationRecordDto FromEntity(IterationRecord r) => new(
        r.IterationNumber,
        r.DeveloperSummaryJson,
        r.ReviewerVerdictJson,
        r.CreatedAt);
}

public record BacklogTaskDetailDto(BacklogTaskDto Task, List<IterationRecordDto> Iterations);
