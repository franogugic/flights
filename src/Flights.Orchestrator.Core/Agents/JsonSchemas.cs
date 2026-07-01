using System.Text.Json;

namespace Flights.Orchestrator.Core.Agents;

/// <summary>
/// Raw JSON Schemas for each role's final structured-output call. Written as plain anonymous
/// objects for readability, then flattened into the Dictionary&lt;string, JsonElement&gt; shape
/// OutputConfig.Format.Schema expects.
/// </summary>
public static class JsonSchemas
{
    public static Dictionary<string, JsonElement> ToSchemaDictionary(object schema)
    {
        var element = JsonSerializer.SerializeToElement(schema);
        return element.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
    }

    public static readonly Dictionary<string, JsonElement> ArchitectBacklog = ToSchemaDictionary(new
    {
        type = "object",
        properties = new
        {
            tasks = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "Stable task id, e.g. T-001." },
                        description = new { type = "string" },
                        acceptanceCriteria = new { type = "string" },
                        dependsOn = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "Ids of tasks that must be Done before this one can start."
                        }
                    },
                    required = new[] { "id", "description", "acceptanceCriteria", "dependsOn" },
                    additionalProperties = false
                }
            }
        },
        required = new[] { "tasks" },
        additionalProperties = false
    });

    public static readonly Dictionary<string, JsonElement> DeveloperSummary = ToSchemaDictionary(new
    {
        type = "object",
        properties = new
        {
            whatWasDone = new { type = "string" },
            buildPassed = new { type = "boolean" },
            testsPassed = new { type = "boolean" },
            deviations = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        description = new { type = "string" },
                        justification = new { type = "string" }
                    },
                    required = new[] { "description", "justification" },
                    additionalProperties = false
                }
            }
        },
        required = new[] { "whatWasDone", "buildPassed", "testsPassed", "deviations" },
        additionalProperties = false
    });

    public static readonly Dictionary<string, JsonElement> ReviewerVerdict = ToSchemaDictionary(new
    {
        type = "object",
        properties = new
        {
            verdict = new { type = "string", @enum = new[] { "APPROVE", "CHANGES_REQUESTED" } },
            findings = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        file = new { type = "string" },
                        issue = new { type = "string" },
                        suggestedFix = new { type = "string" }
                    },
                    required = new[] { "file", "issue", "suggestedFix" },
                    additionalProperties = false
                }
            }
        },
        required = new[] { "verdict", "findings" },
        additionalProperties = false
    });
}
