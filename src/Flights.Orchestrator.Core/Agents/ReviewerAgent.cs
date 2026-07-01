using Anthropic;
using Anthropic.Models.Messages;
using Flights.Orchestrator.Core.Tools;

namespace Flights.Orchestrator.Core.Agents;

/// <summary>
/// Reviews the Developer's work against the task's acceptance criteria. Read-only by construction —
/// write_file/edit_file are simply not registered for this role, not just prompted against.
/// </summary>
public class ReviewerAgent
{
    private const string SystemPrompt = """
        You are the Reviewer on a small automated software team. You check the Developer's work
        against the task's acceptance criteria before it's considered done.

        Rules:
        - You cannot edit code — you only have read, search, and build/test tools. Report findings;
          never try to fix things yourself.
        - Verify the acceptance criteria are actually met, not just that the Developer claims they are.
          Run the build/tests yourself rather than trusting the Developer's summary.
        - Give APPROVE only when the acceptance criteria are genuinely satisfied.
        - When requesting changes, give concrete, actionable findings (file, issue, suggested fix) —
          not vague feedback.
        """;

    private readonly AgentLoopRunner _loopRunner;

    public ReviewerAgent(AnthropicClient client, PathSandbox workspaceSandbox)
    {
        IReadOnlyList<IAgentTool> tools =
        [
            new ReadFileTool(workspaceSandbox),
            new ListDirectoryTool(workspaceSandbox),
            new GrepTool(workspaceSandbox),
            new RunShellCommandTool(workspaceSandbox),
        ];
        _loopRunner = new AgentLoopRunner(client, tools);
    }

    public async Task<ReviewerResult> RunAsync(
        string taskDescription,
        string acceptanceCriteria,
        string developerSummary,
        CancellationToken cancellationToken = default)
    {
        var userMessage = $"""
            Task: {taskDescription}

            Acceptance criteria: {acceptanceCriteria}

            The Developer reports:
            {developerSummary}

            Review the actual code in the workspace against the acceptance criteria and give your verdict.
            """;

        var initialMessages = new List<MessageParam>
        {
            new() { Role = Role.User, Content = userMessage },
        };

        var loopResult = await _loopRunner.RunToolLoopAsync(
            Model.ClaudeOpus4_8, SystemPrompt, initialMessages, Effort.High, cancellationToken);

        return await _loopRunner.RequestStructuredOutputAsync<ReviewerResult>(
            Model.ClaudeOpus4_8,
            SystemPrompt,
            loopResult.Messages,
            "Give your final verdict now as structured JSON.",
            JsonSchemas.ReviewerVerdict,
            cancellationToken);
    }
}
