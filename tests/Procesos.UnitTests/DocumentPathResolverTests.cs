using Microsoft.Extensions.Options;
using Procesos.Data.Storage;

namespace Procesos.UnitTests;

/// <summary>Path-traversal guard for physical documents (ADR-0010, NFR-04).</summary>
public sealed class DocumentPathResolverTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "procesos-tests", Guid.NewGuid().ToString("N"));

    private DocumentPathResolver CreateResolver() =>
        new(Options.Create(new DocumentStorageOptions { RootPath = _root }));

    [Theory]
    [InlineData("EXP-2024-00031/01_Demanda.txt")]
    [InlineData("EXP-2024-00031\\01_Demanda.txt")]
    [InlineData("a/b/../c.txt")]
    public void ResolveFullPath_ReturnsPathInsideRoot_ForValidRelativePaths(string relative)
    {
        var resolved = CreateResolver().ResolveFullPath(relative);

        Assert.StartsWith(Path.GetFullPath(_root) + Path.DirectorySeparatorChar, resolved, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("../secrets.json")]
    [InlineData("EXP-2024-00031/../../outside.txt")]
    public void ResolveFullPath_Throws_WhenPathEscapesRoot(string relative)
    {
        Assert.Throws<UnauthorizedAccessException>(() => CreateResolver().ResolveFullPath(relative));
    }

    [Fact]
    public void ResolveFullPath_Throws_ForBackslashTraversal_OnWindows()
    {
        // On Linux "\" is a valid file-name character, so this is only a traversal on Windows.
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.Throws<UnauthorizedAccessException>(() => CreateResolver().ResolveFullPath("..\\..\\Windows\\win.ini"));
    }

    [Fact]
    public void ResolveFullPath_Throws_ForAbsolutePathOutsideRoot()
    {
        var outside = Path.Combine(Path.GetTempPath(), "elsewhere", "file.txt");

        Assert.Throws<UnauthorizedAccessException>(() => CreateResolver().ResolveFullPath(outside));
    }

    [Fact]
    public void ResolveFullPath_Throws_ForSiblingDirectoryWithSamePrefix()
    {
        // "<root>-evil/x.txt" shares the textual prefix of "<root>" but is outside it.
        var sibling = $"../{Path.GetFileName(_root)}-evil/x.txt";

        Assert.Throws<UnauthorizedAccessException>(() => CreateResolver().ResolveFullPath(sibling));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveFullPath_Throws_ForEmptyPath(string relative)
    {
        Assert.Throws<ArgumentException>(() => CreateResolver().ResolveFullPath(relative));
    }

    [Fact]
    public void Constructor_Throws_WhenRootNotConfigured()
    {
        Assert.Throws<InvalidOperationException>(
            () => new DocumentPathResolver(Options.Create(new DocumentStorageOptions())));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
