using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Anthropic.Models.Messages;

namespace Flights.Orchestrator.Core.Tools;

public class GrepTool(PathSandbox sandbox) : IAgentTool
{
    private const int MaxMatches = 200;

    public string Name => "grep";

    public Tool Definition => new()
    {
        Name = Name,
        Description = "Search for a regex pattern across text files under a path within the sandboxed directory (recursive).",
        InputSchema = new InputSchema
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["pattern"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = ".NET regular expression to search for."
                }),
                ["path"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "Directory to search under, relative to the sandbox root. Defaults to the sandbox root."
                })
            },
            Required = ["pattern"]
        }
    };

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, CancellationToken cancellationToken = default)
    {
        var pattern = input.GetProperty("pattern").GetString()!;
        var relativePath = input.TryGetProperty("path", out var pathProp) ? pathProp.GetString() ?? "." : ".";

        Regex regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolExecutionResult.Error($"Invalid regex pattern: {ex.Message}"));
        }

        try
        {
            var fullPath = sandbox.ResolveSafe(relativePath);
            if (!Directory.Exists(fullPath))
            {
                return Task.FromResult(ToolExecutionResult.Error($"Directory not found: {relativePath}"));
            }

            var sb = new StringBuilder();
            var matchCount = 0;

            foreach (var file in Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories))
            {
                // Skip build output and VCS folders — noise, not source.
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                    file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                    file.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}") ||
                    file.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                string[] lines;
                try
                {
                    lines = File.ReadAllLines(file);
                }
                catch
                {
                    continue; // binary or unreadable file — skip
                }

                for (var i = 0; i < lines.Length && matchCount < MaxMatches; i++)
                {
                    if (regex.IsMatch(lines[i]))
                    {
                        var relative = Path.GetRelativePath(sandbox.RootDirectory, file);
                        sb.AppendLine($"{relative}:{i + 1}: {lines[i].Trim()}");
                        matchCount++;
                    }
                }

                if (matchCount >= MaxMatches)
                {
                    sb.AppendLine($"(truncated at {MaxMatches} matches)");
                    break;
                }
            }

            var result = sb.ToString();
            return Task.FromResult(ToolExecutionResult.Ok(result.Length == 0 ? "(no matches)" : result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Task.FromResult(ToolExecutionResult.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolExecutionResult.Error($"Grep failed: {ex.Message}"));
        }
    }
}
