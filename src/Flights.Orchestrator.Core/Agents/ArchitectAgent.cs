using Anthropic;
using Anthropic.Models.Messages;
using Flights.Orchestrator.Core.Tools;

namespace Flights.Orchestrator.Core.Agents;

/// <summary>
/// Team-lead role. Reads the project brief, breaks it into a concrete, dependency-ordered backlog
/// of tasks for the Developer/Reviewer loop. Read-only — never writes code itself. If it hits a
/// genuine product/requirements ambiguity it cannot reasonably decide on its own, it calls
/// emit_needs_input instead of finishing the backlog.
/// </summary>
public class ArchitectAgent
{
    private const string SystemPrompt = """
        You are the Architect on a small automated software team. You act as team lead: you read a
        project brief and break it into a concrete, ordered backlog of implementation tasks for a
        Developer agent, which a Reviewer agent will later check against your acceptance criteria.

        Rules:
        - You are read-only. You never write or edit code yourself.
        - Each task must be small enough for one Developer iteration, have a clear description, and
          concrete, checkable acceptance criteria.
        - Order tasks with explicit dependencies (dependsOn) where one task's output is needed by another.
        - Do not invent requirements the brief doesn't support, and do not silently guess on a genuine
          ambiguity that changes the product's behavior. If you hit one, call emit_needs_input with one
          specific, concrete question instead of finishing the backlog.
        - Coding-level decisions (which library, which internal structure) are yours to make — only
          escalate product/requirements ambiguities a human needs to decide.
        """;

    private readonly AgentLoopRunner _loopRunner;

    public ArchitectAgent(AnthropicClient client, PathSandbox repoRootSandbox)
    {
        IReadOnlyList<IAgentTool> tools =
        [
            new ReadFileTool(repoRootSandbox),
            new ListDirectoryTool(repoRootSandbox),
            new GrepTool(repoRootSandbox),
            new EmitNeedsInputTool(),
        ];
        _loopRunner = new AgentLoopRunner(client, tools);
    }

    public async Task<ArchitectResult> RunAsync(
        string projectBrief,
        string? priorQuestionAndAnswer,
        CancellationToken cancellationToken = default)
    {
        var userMessage = $"""
            Here is the project brief:

            ---
            {projectBrief}
            ---
            {(priorQuestionAndAnswer is null ? "" : $"\nA human previously answered a clarifying question:\n{priorQuestionAndAnswer}\n")}
            Break this down into a backlog of concrete, ordered, dependency-aware implementation tasks.
            If you need a human decision on a genuine ambiguity, call emit_needs_input instead.
            """;

        var initialMessages = new List<MessageParam>
        {
            new() { Role = Role.User, Content = userMessage },
        };

        var loopResult = await _loopRunner.RunToolLoopAsync(
            Model.ClaudeOpus4_8, SystemPrompt, initialMessages, Effort.High, cancellationToken);

        if (loopResult.IsNeedsInput)
        {
            return new ArchitectResult { NeedsInputQuestion = loopResult.NeedsInputQuestion };
        }

        var backlog = await _loopRunner.RequestStructuredOutputAsync<ArchitectBacklogDto>(
            Model.ClaudeOpus4_8,
            SystemPrompt,
            loopResult.Messages,
            "Summarize your final backlog now as structured JSON.",
            JsonSchemas.ArchitectBacklog,
            cancellationToken);

        return new ArchitectResult { Tasks = backlog.Tasks };
    }
}
