using Flights.Orchestrator.Core.Models;

namespace Flights.Orchestrator.Api.Dtos;

public record PendingQuestionDto(
    string Id,
    string ProjectName,
    string? BacklogTaskId,
    string Source,
    string QuestionText,
    string Status,
    string? AnswerText,
    DateTime CreatedAt,
    DateTime? AnsweredAt)
{
    public static PendingQuestionDto FromEntity(PendingQuestion q) => new(
        q.Id,
        q.ProjectName,
        q.BacklogTaskId,
        q.Source.ToString(),
        q.QuestionText,
        q.Status.ToString(),
        q.AnswerText,
        q.CreatedAt,
        q.AnsweredAt);
}

public record AnswerQuestionRequest(string AnswerText);
