using System.Text.Json.Serialization;

namespace Flights.Orchestrator.Core.Agents;

public enum ReviewerVerdict
{
    Approve,
    ChangesRequested
}

public class ReviewerFindingDto
{
    [JsonPropertyName("file")]
    public string File { get; set; } = default!;

    [JsonPropertyName("issue")]
    public string Issue { get; set; } = default!;

    [JsonPropertyName("suggestedFix")]
    public string SuggestedFix { get; set; } = default!;
}

public class ReviewerResult
{
    [JsonPropertyName("verdict")]
    public string VerdictRaw { get; set; } = default!;

    [JsonPropertyName("findings")]
    public List<ReviewerFindingDto> Findings { get; set; } = new();

    [JsonIgnore]
    public ReviewerVerdict Verdict =>
        VerdictRaw.Equals("APPROVE", StringComparison.OrdinalIgnoreCase)
            ? ReviewerVerdict.Approve
            : ReviewerVerdict.ChangesRequested;
}
