using Anthropic.Models.Messages;

namespace Flights.Orchestrator.Core.Agents;

/// <summary>
/// Outcome of the tool-use portion of an agent run: either the model finished acting
/// (ready for the final structured-output call) or it raised a NEEDS_INPUT question
/// (via <see cref="Tools.EmitNeedsInputTool"/>), in which case the run stops immediately.
/// </summary>
public class AgentLoopResult
{
    public required List<MessageParam> Messages { get; init; }
    public string? NeedsInputQuestion { get; init; }

    public bool IsNeedsInput => NeedsInputQuestion is not null;
}
