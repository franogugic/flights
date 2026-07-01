using Flights.Orchestrator.Core.Tools;

namespace Flights.Orchestrator.Core.Tests;

public class PathSandboxTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly PathSandbox _sandbox;

    public PathSandboxTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "sandbox-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
        _sandbox = new PathSandbox(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ResolveSafe_AllowsRelativePathInsideRoot()
    {
        var resolved = _sandbox.ResolveSafe("subdir/file.txt");

        Assert.StartsWith(_tempRoot, resolved, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveSafe_AllowsRootItself()
    {
        var resolved = _sandbox.ResolveSafe(".");

        Assert.Equal(Path.GetFullPath(_tempRoot), resolved);
    }

    [Fact]
    public void ResolveSafe_RejectsDotDotTraversal()
    {
        Assert.Throws<UnauthorizedAccessException>(() => _sandbox.ResolveSafe("../outside.txt"));
    }

    [Fact]
    public void ResolveSafe_RejectsDeepDotDotTraversal()
    {
        Assert.Throws<UnauthorizedAccessException>(() => _sandbox.ResolveSafe("a/b/../../../outside.txt"));
    }

    [Fact]
    public void ResolveSafe_RejectsAbsolutePathOutsideRoot()
    {
        var outsidePath = Path.Combine(Path.GetTempPath(), "definitely-outside-" + Guid.NewGuid());

        Assert.Throws<UnauthorizedAccessException>(() => _sandbox.ResolveSafe(outsidePath));
    }

    [Fact]
    public void ResolveSafe_AllowsAbsolutePathInsideRoot()
    {
        var insidePath = Path.Combine(_tempRoot, "nested", "file.txt");

        var resolved = _sandbox.ResolveSafe(insidePath);

        Assert.Equal(Path.GetFullPath(insidePath), resolved);
    }

    [Fact]
    public void ResolveSafe_RejectsEmptyPath()
    {
        Assert.Throws<UnauthorizedAccessException>(() => _sandbox.ResolveSafe(""));
    }

    [Fact]
    public void ResolveSafe_RejectsSymlinkEscapingRoot()
    {
        var outsideDir = Path.Combine(Path.GetTempPath(), "symlink-target-" + Guid.NewGuid());
        Directory.CreateDirectory(outsideDir);
        try
        {
            var outsideFile = Path.Combine(outsideDir, "secret.txt");
            File.WriteAllText(outsideFile, "should not be reachable");

            var symlinkPath = Path.Combine(_tempRoot, "escape-link");
            File.CreateSymbolicLink(symlinkPath, outsideFile);

            Assert.Throws<UnauthorizedAccessException>(() => _sandbox.ResolveSafe("escape-link"));
        }
        finally
        {
            Directory.Delete(outsideDir, recursive: true);
        }
    }

    [Fact]
    public void ResolveSafe_AllowsSymlinkStayingInsideRoot()
    {
        var targetDir = Path.Combine(_tempRoot, "real-target");
        Directory.CreateDirectory(targetDir);
        var targetFile = Path.Combine(targetDir, "file.txt");
        File.WriteAllText(targetFile, "fine");

        var symlinkPath = Path.Combine(_tempRoot, "internal-link");
        File.CreateSymbolicLink(symlinkPath, targetFile);

        var resolved = _sandbox.ResolveSafe("internal-link");

        Assert.StartsWith(_tempRoot, resolved, StringComparison.Ordinal);
    }
}
