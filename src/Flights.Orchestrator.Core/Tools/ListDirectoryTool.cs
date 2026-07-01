using System.Text;
using System.Text.Json;
using Anthropic.Models.Messages;

namespace Flights.Orchestrator.Core.Tools;

public class ListDirectoryTool(PathSandbox sandbox) : IAgentTool
{
    public string Name => "list_directory";

    public Tool Definition => new()
    {
        Name = Name,
        Description = "List files and subdirectories at a path within the sandboxed directory (non-recursive).",
        InputSchema = new InputSchema
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["path"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "Directory path, relative to the sandbox root. Use \".\" for the root."
                })
            },
            Required = ["path"]
        }
    };

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, CancellationToken cancellationToken = default)
    {
        var relativePath = input.GetProperty("path").GetString() ?? ".";

        try
        {
            var fullPath = sandbox.ResolveSafe(relativePath);

            if (!Directory.Exists(fullPath))
            {
                return Task.FromResult(ToolExecutionResult.Error($"Directory not found: {relativePath}"));
            }

            var sb = new StringBuilder();
            foreach (var dir in Directory.GetDirectories(fullPath).OrderBy(d => d))
            {
                sb.AppendLine($"{Path.GetFileName(dir)}/");
            }

            foreach (var file in Directory.GetFiles(fullPath).OrderBy(f => f))
            {
                sb.AppendLine(Path.GetFileName(file));
            }

            var listing = sb.ToString();
            return Task.FromResult(ToolExecutionResult.Ok(listing.Length == 0 ? "(empty directory)" : listing));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Task.FromResult(ToolExecutionResult.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolExecutionResult.Error($"Failed to list '{relativePath}': {ex.Message}"));
        }
    }
}
