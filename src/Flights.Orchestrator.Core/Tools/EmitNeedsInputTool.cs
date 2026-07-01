using System.Text.Json;
using Anthropic.Models.Messages;

namespace Flights.Orchestrator.Core.Tools;

/// <summary>
/// Architect-only tool: called instead of finishing the backlog when a genuine product/requirements
/// ambiguity needs a human decision (not something the Architect can reasonably guess).
///
/// This tool is registered so the model can see and call it, but <see cref="ArchitectAgent"/>
/// intercepts the tool_use block by name BEFORE the generic tool-dispatch loop would call
/// <see cref="ExecuteAsync"/> — calling this tool ends the Architect's run immediately (a
/// PendingQuestion is persisted and no backlog is produced this run), so there is nothing
/// meaningful to feed back as a tool_result. ExecuteAsync exists only to satisfy IAgentTool and
/// is not expected to be invoked in normal operation.
/// </summary>
public class EmitNeedsInputTool : IAgentTool
{
    public const string ToolName = "emit_needs_input";

    public string Name => ToolName;

    public Tool Definition => new()
    {
        Name = Name,
        Description = "Call this INSTEAD of finishing your backlog if you hit a genuine ambiguity that " +
                       "requires a human decision — not a coding detail you can reasonably decide yourself. " +
                       "Ask one specific, concrete question.",
        InputSchema = new InputSchema
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["question"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "One specific, concrete question for the human to answer."
                })
            },
            Required = ["question"]
        }
    };

    public static string ExtractQuestion(JsonElement input) => input.GetProperty("question").GetString()!;

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, CancellationToken cancellationToken = default) =>
        Task.FromResult(ToolExecutionResult.Ok("Question recorded."));
}
