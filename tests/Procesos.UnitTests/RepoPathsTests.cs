using Procesos.Data;

namespace Procesos.UnitTests;

public sealed class RepoPathsTests
{
    [Fact]
    public void FindRepoRoot_LocatesSolutionFile_FromTestOutputDirectory()
    {
        var root = RepoPaths.FindRepoRoot();

        Assert.True(File.Exists(Path.Combine(root, "ProcesosLegalesLab.slnx")));
        Assert.True(Directory.Exists(RepoPaths.ResolveMockDocumentsRoot()));
    }

    [Fact]
    public void FindRepoRoot_Throws_WhenNoSolutionAbove()
    {
        var isolated = Directory.CreateTempSubdirectory("procesos-no-sln").FullName;
        try
        {
            Assert.Throws<InvalidOperationException>(() => RepoPaths.FindRepoRoot(isolated));
        }
        finally
        {
            Directory.Delete(isolated);
        }
    }
}
