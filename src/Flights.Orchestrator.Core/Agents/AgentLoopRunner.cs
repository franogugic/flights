using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Flights.Orchestrator.Core.Tools;

namespace Flights.Orchestrator.Core.Agents;

/// <summary>
/// Shared manual agentic tool-use loop used by all three roles (Architect/Developer/Reviewer).
///
/// Shape: run the tool-use loop (call -> execute tool_use blocks -> feed tool_result back) until
/// the model stops calling tools, then issue ONE additional call with no tools and
/// OutputConfig.Format set, asking the model to emit its final answer as strict JSON. Splitting
/// these into two calls avoids fighting between "call tools" and "emit strict JSON" in the same turn.
///
/// If the Architect calls emit_needs_input, the loop stops immediately with a NeedsInput result —
/// there is nothing meaningful to feed back as a tool_result, and no final structured-output call
/// is made this run (see EmitNeedsInputTool for the full rationale).
/// </summary>
public class AgentLoopRunner(AnthropicClient client, IReadOnlyList<IAgentTool> tools)
{
    private const int MaxToolRounds = 40;
    private const int MaxTokensPerTurn = 8192;

    private readonly Dictionary<string, IAgentTool> _toolsByName = tools.ToDictionary(t => t.Name);
    private readonly List<Tool> _toolDefinitions = tools.Select(t => t.Definition).ToList();

    public async Task<AgentLoopResult> RunToolLoopAsync(
        Model model,
        string systemPrompt,
        List<MessageParam> initialMessages,
        Effort effort,
        CancellationToken cancellationToken)
    {
        var messages = new List<MessageParam>(initialMessages);

        for (var round = 0; round < MaxToolRounds; round++)
        {
            var response = await client.Messages.Create(new MessageCreateParams
            {
                Model = model,
                MaxTokens = MaxTokensPerTurn,
                System = systemPrompt,
                Tools = _toolDefinitions.Select(t => (ToolUnion)t).ToList(),
                Thinking = new ThinkingConfigAdaptive(),
                OutputConfig = new OutputConfig { Effort = effort },
                Messages = messages,
            }, cancellationToken: cancellationToken);

            var assistantContent = new List<ContentBlockParam>();
            var toolResults = new List<ContentBlockParam>();
            string? needsInputQuestion = null;

            foreach (var block in response.Content)
            {
                if (block.TryPickText(out TextBlock? text))
                {
                    assistantContent.Add(new TextBlockParam { Text = text.Text });
                }
                else if (block.TryPickThinking(out ThinkingBlock? thinking))
                {
                    assistantContent.Add(new ThinkingBlockParam
                    {
                        Thinking = thinking.Thinking,
                        Signature = thinking.Signature,
                    });
                }
                else if (block.TryPickRedactedThinking(out RedactedThinkingBlock? redacted))
                {
                    assistantContent.Add(new RedactedThinkingBlockParam { Data = redacted.Data });
                }
                else if (block.TryPickToolUse(out ToolUseBlock? toolUse))
                {
                    assistantContent.Add(new ToolUseBlockParam
                    {
                        ID = toolUse.ID,
                        Name = toolUse.Name,
                        Input = toolUse.Input,
                    });

                    if (toolUse.Name == EmitNeedsInputTool.ToolName)
                    {
                        var inputJson = JsonSerializer.SerializeToElement(toolUse.Input);
                        needsInputQuestion = EmitNeedsInputTool.ExtractQuestion(inputJson);
                        continue;
                    }

                    if (!_toolsByName.TryGetValue(toolUse.Name, out var tool))
                    {
                        toolResults.Add(new ToolResultBlockParam
                        {
                            ToolUseID = toolUse.ID,
                            Content = $"Unknown tool: {toolUse.Name}",
                            IsError = true,
                        });
                        continue;
                    }

                    var toolInputJson = JsonSerializer.SerializeToElement(toolUse.Input);
                    var result = await tool.ExecuteAsync(toolInputJson, cancellationToken);
                    toolResults.Add(new ToolResultBlockParam
                    {
                        ToolUseID = toolUse.ID,
                        Content = result.Content,
                        IsError = result.IsError,
                    });
                }
            }

            messages.Add(new MessageParam { Role = Role.Assistant, Content = assistantContent });

            if (needsInputQuestion is not null)
            {
                return new AgentLoopResult { Messages = messages, NeedsInputQuestion = needsInputQuestion };
            }

            if (response.StopReason != StopReason.ToolUse)
            {
                return new AgentLoopResult { Messages = messages };
            }

            messages.Add(new MessageParam { Role = Role.User, Content = toolResults });
        }

        throw new InvalidOperationException(
            $"Agent exceeded the safety cap of {MaxToolRounds} tool-call rounds without finishing.");
    }

    /// <summary>
    /// Issues one additional call with no tools, asking the model to summarize its work from the
    /// preceding tool-use loop as strict JSON matching <paramref name="schema"/>.
    /// </summary>
    public async Task<T> RequestStructuredOutputAsync<T>(
        Model model,
        string systemPrompt,
        List<MessageParam> messages,
        string finalInstruction,
        Dictionary<string, JsonElement> schema,
        CancellationToken cancellationToken)
    {
        var followUpMessages = new List<MessageParam>(messages)
        {
            new() { Role = Role.User, Content = finalInstruction },
        };

        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = model,
            MaxTokens = MaxTokensPerTurn,
            System = systemPrompt,
            Thinking = new ThinkingConfigDisabled(),
            OutputConfig = new OutputConfig
            {
                Format = new JsonOutputFormat { Schema = schema },
            },
            Messages = followUpMessages,
        }, cancellationToken: cancellationToken);

        var jsonText = response.Content
            .Select(block => block.TryPickText(out TextBlock? text) ? text.Text : null)
            .FirstOrDefault(t => t is not null)
            ?? throw new InvalidOperationException("Structured-output call returned no text content.");

        return JsonSerializer.Deserialize<T>(jsonText)
               ?? throw new InvalidOperationException("Failed to deserialize structured output.");
    }
}
