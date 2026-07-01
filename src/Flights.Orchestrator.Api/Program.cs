using Flights.Orchestrator.Api.Dtos;
using Flights.Orchestrator.Core;
using Flights.Orchestrator.Core.Models;
using Flights.Orchestrator.Core.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string DevCorsPolicy = "orchestrator-ui-dev";

var repoRoot = OrchestratorPaths.FindRepoRoot();
var dbPath = OrchestratorPaths.OrchestratorDbPath(repoRoot);
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContext<OrchestratorDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddCors(options =>
{
    // Personal, local-only tool: no auth, so CORS just needs to let the local Vite dev server
    // (default http://localhost:5173) talk to this API — not a public-facing policy.
    options.AddPolicy(DevCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors(DevCorsPolicy);

app.MapGet("/", () => "Flights.Orchestrator.Api is running.");

app.MapGet("/api/questions", async (OrchestratorDbContext db, string? status) =>
{
    var query = db.PendingQuestions.AsQueryable();

    if (!string.IsNullOrWhiteSpace(status) &&
        Enum.TryParse<PendingQuestionStatus>(status, ignoreCase: true, out var parsedStatus))
    {
        query = query.Where(q => q.Status == parsedStatus);
    }

    var questions = await query
        .OrderByDescending(q => q.CreatedAt)
        .Select(q => PendingQuestionDto.FromEntity(q))
        .ToListAsync();

    return Results.Ok(questions);
});

app.MapGet("/api/questions/{id}", async (OrchestratorDbContext db, string id) =>
{
    var question = await db.PendingQuestions.FindAsync(id);
    return question is null ? Results.NotFound() : Results.Ok(PendingQuestionDto.FromEntity(question));
});

app.MapPost("/api/questions/{id}/answer", async (OrchestratorDbContext db, string id, AnswerQuestionRequest request) =>
{
    var question = await db.PendingQuestions.FindAsync(id);
    if (question is null)
    {
        return Results.NotFound();
    }

    if (string.IsNullOrWhiteSpace(request.AnswerText))
    {
        return Results.BadRequest("answerText must not be empty.");
    }

    question.AnswerText = request.AnswerText;
    question.Status = PendingQuestionStatus.Answered;
    question.AnsweredAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(PendingQuestionDto.FromEntity(question));
});

app.MapGet("/api/backlog", async (OrchestratorDbContext db) =>
{
    var tasks = await db.BacklogTasks
        .OrderBy(t => t.CreatedAt)
        .Select(t => BacklogTaskDto.FromEntity(t))
        .ToListAsync();

    return Results.Ok(tasks);
});

app.MapGet("/api/backlog/{id}", async (OrchestratorDbContext db, string id) =>
{
    var task = await db.BacklogTasks.FindAsync(id);
    if (task is null)
    {
        return Results.NotFound();
    }

    var iterations = await db.IterationRecords
        .Where(r => r.BacklogTaskId == id)
        .OrderBy(r => r.IterationNumber)
        .Select(r => IterationRecordDto.FromEntity(r))
        .ToListAsync();

    return Results.Ok(new BacklogTaskDetailDto(BacklogTaskDto.FromEntity(task), iterations));
});

app.Run();
