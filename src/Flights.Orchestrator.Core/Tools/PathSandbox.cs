namespace Flights.Orchestrator.Core.Tools;

/// <summary>
/// Confines file-touching tool calls to a fixed root directory. Every model-supplied path is
/// resolved to its canonical form and rejected if it escapes the root — closes off both plain
/// ".." traversal and symlink-based escapes.
/// </summary>
public class PathSandbox(string rootDirectory)
{
    private readonly string _root = Path.GetFullPath(rootDirectory);

    public string RootDirectory => _root;

    /// <summary>
    /// Resolves a model-supplied path (relative or absolute) against the sandbox root and
    /// throws if the resolved, canonical path escapes it.
    /// </summary>
    public string ResolveSafe(string relativeOrAbsolutePath)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
        {
            throw new UnauthorizedAccessException("Path must not be empty.");
        }

        var combined = Path.IsPathRooted(relativeOrAbsolutePath)
            ? relativeOrAbsolutePath
            : Path.Combine(_root, relativeOrAbsolutePath);

        var full = Path.GetFullPath(combined);
        EnsureWithinRoot(full, relativeOrAbsolutePath);

        // If the path (or an ancestor, for a not-yet-created file) is a symlink, resolve the
        // real target and check that too — a symlink inside the sandbox can still point outside it.
        var realTarget = ResolveRealPathIfExists(full);
        if (realTarget is not null)
        {
            EnsureWithinRoot(realTarget, relativeOrAbsolutePath);
        }

        return full;
    }

    private void EnsureWithinRoot(string full, string originalInput)
    {
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (full != _root && !full.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException(
                $"Path '{originalInput}' resolves to '{full}', which escapes the sandbox root '{_root}'.");
        }
    }

    private static string? ResolveRealPathIfExists(string path)
    {
        if (File.Exists(path))
        {
            return new FileInfo(path).ResolveLinkTarget(returnFinalTarget: true)?.FullName
                   ?? Path.GetFullPath(path);
        }

        if (Directory.Exists(path))
        {
            return new DirectoryInfo(path).ResolveLinkTarget(returnFinalTarget: true)?.FullName
                   ?? Path.GetFullPath(path);
        }

        // Path doesn't exist yet (e.g. a file about to be created) — nothing further to resolve.
        return null;
    }
}
