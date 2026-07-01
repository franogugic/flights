using System.Text.Json;
using Anthropic.Models.Messages;

namespace Flights.Orchestrator.Core.Tools;

public class EditFileTool(PathSandbox sandbox) : IAgentTool
{
    public string Name => "edit_file";

    public Tool Definition => new()
    {
        Name = Name,
        Description = "Replace an exact, unique occurrence of old_str with new_str in a file within the sandboxed directory. " +
                       "Errors if old_str appears zero or more than one time.",
        InputSchema = new InputSchema
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["path"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "Path to the file, relative to the sandbox root."
                }),
                ["old_str"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "Exact text to find. Must match exactly once in the file."
                }),
                ["new_str"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "Text to replace old_str with."
                })
            },
            Required = ["path", "old_str", "new_str"]
        }
    };

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, CancellationToken cancellationToken = default)
    {
        var relativePath = input.GetProperty("path").GetString()!;
        var oldStr = input.GetProperty("old_str").GetString() ?? string.Empty;
        var newStr = input.GetProperty("new_str").GetString() ?? string.Empty;

        try
        {
            var fullPath = sandbox.ResolveSafe(relativePath);

            if (!File.Exists(fullPath))
            {
                return ToolExecutionResult.Error($"File not found: {relativePath}");
            }

            var content = await File.ReadAllTextAsync(fullPath, cancellationToken);

            var firstIndex = content.IndexOf(oldStr, StringComparison.Ordinal);
            if (firstIndex < 0)
            {
                return ToolExecutionResult.Error($"old_str not found in '{relativePath}'.");
            }

            var lastIndex = content.LastIndexOf(oldStr, StringComparison.Ordinal);
            if (lastIndex != firstIndex)
            {
                return ToolExecutionResult.Error(
                    $"old_str matches more than once in '{relativePath}'. Provide more surrounding context to make it unique.");
            }

            var updated = string.Concat(content.AsSpan(0, firstIndex), newStr, content.AsSpan(firstIndex + oldStr.Length));
            await File.WriteAllTextAsync(fullPath, updated, cancellationToken);

            return ToolExecutionResult.Ok($"Replaced 1 occurrence in {relativePath}.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolExecutionResult.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolExecutionResult.Error($"Failed to edit '{relativePath}': {ex.Message}");
        }
    }
}
