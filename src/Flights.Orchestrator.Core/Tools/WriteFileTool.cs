using System.Text.Json;
using Anthropic.Models.Messages;

namespace Flights.Orchestrator.Core.Tools;

public class WriteFileTool(PathSandbox sandbox) : IAgentTool
{
    public string Name => "write_file";

    public Tool Definition => new()
    {
        Name = Name,
        Description = "Create or overwrite a text file within the sandboxed directory. Creates parent directories as needed.",
        InputSchema = new InputSchema
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["path"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "Path to the file, relative to the sandbox root."
                }),
                ["content"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "Full contents to write to the file."
                })
            },
            Required = ["path", "content"]
        }
    };

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, CancellationToken cancellationToken = default)
    {
        var relativePath = input.GetProperty("path").GetString()!;
        var content = input.GetProperty("content").GetString() ?? string.Empty;

        try
        {
            var fullPath = sandbox.ResolveSafe(relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content, cancellationToken);
            return ToolExecutionResult.Ok($"Wrote {content.Length} characters to {relativePath}.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolExecutionResult.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolExecutionResult.Error($"Failed to write '{relativePath}': {ex.Message}");
        }
    }
}
