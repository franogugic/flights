using System.Text.Json;
using Anthropic.Models.Messages;

namespace Flights.Orchestrator.Core.Tools;

public record ToolExecutionResult(string Content, bool IsError = false)
{
    public static ToolExecutionResult Ok(string content) => new(content);
    public static ToolExecutionResult Error(string message) => new(message, IsError: true);
}

/// <summary>
/// A single client-side tool: its Claude API schema plus the local code that executes it.
/// </summary>
public interface IAgentTool
{
    string Name { get; }

    Tool Definition { get; }

    Task<ToolExecutionResult> ExecuteAsync(JsonElement input, CancellationToken cancellationToken = default);
}
