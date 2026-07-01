using Anthropic;
using Anthropic.Models.Messages;
using Flights.Orchestrator.Core.Tools;

namespace Flights.Orchestrator.Core.Agents;

/// <summary>
/// Implements one backlog task at a time inside the workspace/ sandbox. On a retry, receives the
/// Reviewer's findings from the previous iteration as extra context for the SAME task.
/// </summary>
public class DeveloperAgent
{
    private const string SystemPrompt = """
        You are the Developer on a small automated software team. You implement exactly one backlog
        task at a time inside your sandboxed working directory.

        Rules:
        - Implement only what the task describes. Don't add features, refactor, or introduce
          abstractions beyond what the task requires.
        - Run the project's build and tests before reporting you are done. Report honestly whether
          they passed — do not claim success you have not verified with a tool call.
        - If you must deviate from the task description, do so with a clear justification, not silently.
        - You have no knowledge of what tech stack the eventual product uses beyond what's already in
          the workspace and what the task tells you — decide pragmatically and note the choice.
        """;

    private readonly AgentLoopRunner _loopRunner;

    public DeveloperAgent(AnthropicClient client, PathSandbox workspaceSandbox)
    {
        IReadOnlyList<IAgentTool> tools =
        [
            new ReadFileTool(workspaceSandbox),
            new WriteFileTool(workspaceSandbox),
            new EditFileTool(workspaceSandbox),
            new ListDirectoryTool(workspaceSandbox),
            new GrepTool(workspaceSandbox),
            new RunShellCommandTool(workspaceSandbox),
        ];
        _loopRunner = new AgentLoopRunner(client, tools);
    }

    public async Task<DeveloperResult> RunAsync(
        string taskDescription,
        string acceptanceCriteria,
        string? reviewerFeedback,
        CancellationToken cancellationToken = default)
    {
        var userMessage = $"""
            Task: {taskDescription}

            Acceptance criteria: {acceptanceCriteria}
            {(reviewerFeedback is null ? "" : $"\nThe Reviewer requested changes on your previous attempt:\n{reviewerFeedback}\n\nAddress these findings.\n")}
            Implement this task now.
            """;

        var initialMessages = new List<MessageParam>
        {
            new() { Role = Role.User, Content = userMessage },
        };

        var loopResult = await _loopRunner.RunToolLoopAsync(
            Model.ClaudeSonnet5, SystemPrompt, initialMessages, Effort.High, cancellationToken);

        return await _loopRunner.RequestStructuredOutputAsync<DeveloperResult>(
            Model.ClaudeSonnet5,
            SystemPrompt,
            loopResult.Messages,
            "Summarize what you did now as structured JSON.",
            JsonSchemas.DeveloperSummary,
            cancellationToken);
    }
}
