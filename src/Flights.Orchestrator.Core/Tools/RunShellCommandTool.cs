using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Anthropic.Models.Messages;

namespace Flights.Orchestrator.Core.Tools;

/// <summary>
/// Runs a shell command with its working directory fixed to the sandbox root — the model can
/// supply the command itself, but never the cwd, closing off a path-traversal-via-cwd vector.
/// </summary>
public class RunShellCommandTool(PathSandbox sandbox) : IAgentTool
{
    private const int DefaultTimeoutSeconds = 300;

    public string Name => "run_shell_command";

    public Tool Definition => new()
    {
        Name = Name,
        Description = "Run a shell command with its working directory fixed to the sandboxed root " +
                       "(e.g. `dotnet build`, `dotnet test`, or whatever the project's own build/test tooling is).",
        InputSchema = new InputSchema
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["command"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "The shell command to run."
                }),
                ["timeout_seconds"] = JsonSerializer.SerializeToElement(new
                {
                    type = "integer",
                    description = "Optional timeout in seconds (default 300)."
                })
            },
            Required = ["command"]
        }
    };

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, CancellationToken cancellationToken = default)
    {
        var command = input.GetProperty("command").GetString()!;
        var timeoutSeconds = input.TryGetProperty("timeout_seconds", out var t) && t.TryGetInt32(out var tv)
            ? tv
            : DefaultTimeoutSeconds;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                ArgumentList = { "-c", command },
                WorkingDirectory = sandbox.RootDirectory, // never model-supplied
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            }
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                TryKill(process);
                return ToolExecutionResult.Error(
                    $"Command timed out after {timeoutSeconds}s.\nstdout so far:\n{stdout}\nstderr so far:\n{stderr}");
            }

            var result = $"exit code: {process.ExitCode}\nstdout:\n{stdout}\nstderr:\n{stderr}";
            return process.ExitCode == 0
                ? ToolExecutionResult.Ok(result)
                : ToolExecutionResult.Error(result);
        }
        catch (Exception ex)
        {
            return ToolExecutionResult.Error($"Failed to run command: {ex.Message}");
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
