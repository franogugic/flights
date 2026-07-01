using System.Text.Json;
using Flights.Orchestrator.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Flights.Orchestrator.Core.Persistence;

/// <summary>
/// The orchestrator's own state store (backlog, iteration history, pending questions).
/// This is separate from whatever database the eventual scraper product uses at runtime.
/// </summary>
public class OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options) : DbContext(options)
{
    public DbSet<BacklogTask> BacklogTasks => Set<BacklogTask>();
    public DbSet<IterationRecord> IterationRecords => Set<IterationRecord>();
    public DbSet<PendingQuestion> PendingQuestions => Set<PendingQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var stringListConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        modelBuilder.Entity<BacklogTask>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.DependsOnTaskIds)
                .HasConversion(stringListConverter);
        });

        modelBuilder.Entity<IterationRecord>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.BacklogTaskId);
        });

        modelBuilder.Entity<PendingQuestion>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.HasIndex(q => q.Status);
        });
    }
}
