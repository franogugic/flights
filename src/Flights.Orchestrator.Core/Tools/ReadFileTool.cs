using System.Text.Json;
using Anthropic.Models.Messages;

namespace Flights.Orchestrator.Core.Tools;

public class ReadFileTool(PathSandbox sandbox) : IAgentTool
{
    public string Name => "read_file";

    public Tool Definition => new()
    {
        Name = Name,
        Description = "Read the contents of a text file within the sandboxed directory.",
        InputSchema = new InputSchema
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["path"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "Path to the file, relative to the sandbox root."
                })
            },
            Required = ["path"]
        }
    };

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, CancellationToken cancellationToken = default)
    {
        var relativePath = input.GetProperty("path").GetString()!;

        try
        {
            var fullPath = sandbox.ResolveSafe(relativePath);

            if (!File.Exists(fullPath))
            {
                return ToolExecutionResult.Error($"File not found: {relativePath}");
            }

            var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
            return ToolExecutionResult.Ok(content);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolExecutionResult.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolExecutionResult.Error($"Failed to read '{relativePath}': {ex.Message}");
        }
    }
}
