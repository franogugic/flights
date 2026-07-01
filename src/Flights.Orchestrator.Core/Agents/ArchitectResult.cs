using System.Text.Json.Serialization;

namespace Flights.Orchestrator.Core.Agents;

public class ArchitectBacklogDto
{
    [JsonPropertyName("tasks")]
    public List<ArchitectTaskDto> Tasks { get; set; } = new();
}

public class ArchitectTaskDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;

    [JsonPropertyName("acceptanceCriteria")]
    public string AcceptanceCriteria { get; set; } = default!;

    [JsonPropertyName("dependsOn")]
    public List<string> DependsOn { get; set; } = new();
}

/// <summary>
/// Result of one Architect run: either a backlog, or a NEEDS_INPUT question (never both).
/// </summary>
public class ArchitectResult
{
    public List<ArchitectTaskDto> Tasks { get; init; } = new();
    public string? NeedsInputQuestion { get; init; }

    public bool IsNeedsInput => NeedsInputQuestion is not null;
}
