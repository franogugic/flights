using System.Text.Json.Serialization;

namespace Flights.Orchestrator.Core.Agents;

public class DeveloperDeviationDto
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;

    [JsonPropertyName("justification")]
    public string Justification { get; set; } = default!;
}

public class DeveloperResult
{
    [JsonPropertyName("whatWasDone")]
    public string WhatWasDone { get; set; } = default!;

    [JsonPropertyName("buildPassed")]
    public bool BuildPassed { get; set; }

    [JsonPropertyName("testsPassed")]
    public bool TestsPassed { get; set; }

    [JsonPropertyName("deviations")]
    public List<DeveloperDeviationDto> Deviations { get; set; } = new();
}
