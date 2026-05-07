namespace JJ.AutoIncrementVersion.Tests;

/// <summary>
/// Helpers for running dotnet CLI commands, manipulating project files,
/// and inspecting build output — used by the automated test-plan tests.
/// Logs the actions to Console or Debug output.
/// Each instance creates an isolated copy of the test files under a random
/// temp folder so that tests do not interfere with each other or the repo.
/// </summary>
public abstract partial class TestBase : IDisposable
{
    #if NCRUNCH
    private const int DEFAULT_REPEATS = 2;
    #else
    private const int DEFAULT_REPEATS = 3;
    #endif

    // Paths

    private string SolutionDir { get; }
    private string ProjectDir { get; }
    private string CsprojFilePath { get; }
    private string BuildNumXmlFilePath { get; }
    private string DirPropsFilePath { get; }

    private const string PackageId = "JJ.AutoIncrementVersion";
    private const string TestProjectName = "JJ.AutoIncrementVersion.Dummy";

    // Init / Cleanup

    internal TestBase()
    {
        // Create a random isolated folder in the system temp directory
        // (outside the repo tree, so MSBuild won't pick up the repo's Directory.Build.props).
        SolutionDir         = Path.Combine(Path.GetTempPath(), "JJ.AutoIncrementVersion.TestRuns", Path.GetRandomFileName().Replace(".", ""));
        ProjectDir          = Path.Combine(SolutionDir, TestProjectName);
        CsprojFilePath      = Path.Combine(ProjectDir, $"{TestProjectName}.csproj");
        BuildNumXmlFilePath = Path.Combine(SolutionDir, "BuildNum.xml");
        DirPropsFilePath    = Path.Combine(SolutionDir, "Directory.Build.props");
        InitDotNetOptions();
    }

    /// <summary>
    /// Re-extracts all embedded test files to the isolated folder,
    /// restoring them to their original state (replaces git restore).
    /// </summary>
    internal void InitInstalledState()
    {
        Log("Init installed state");
        CreateDir(SolutionDir);
        CreateDir(ProjectDir);
        InitCsproj();
        InitBuildNumXml();
        InitDirectoryBuildProps();
        InitDummyTxt();
        InitReadMe();
        InitNuGetConfig();
        Restore();
        Log("Init done");
    }

    /// <summary>
    /// Full "Set Initial State" as described in the manual test plan:
    /// uninstall package, delete BuildNum.xml &amp; Directory.Build.props,
    /// replace $(BuildNum) with 0 in csproj.
    /// </summary>
    internal void InitUninstalled()
    {
        Log("Init uninstalled");
        CreateDir(SolutionDir);
        CreateDir(ProjectDir);
        InitCsproj();
        InitDummyTxt();
        InitReadMe();
        InitNuGetConfig();
        RemovePackageReferenceFromCsproj();
        SetProjPatchNum("0");
        Restore();
        Log("Init done");
    }

    private void CreateDir(string path)
    {
        Log($"Create dir => {path}");
        Directory.CreateDirectory(path);
    }

    private void InitCsproj()              => ExtractResource(ProjectDir, TestProjectName + ".csproj");
    private void InitDirectoryBuildProps() => ExtractResource(SolutionDir, "Directory.Build.props");
    private void InitBuildNumXml()         => ExtractResource(SolutionDir, "BuildNum.xml");
    private void InitDummyTxt()            => ExtractResource(ProjectDir, "Dummy.txt");
    private void InitReadMe()              => ExtractResource(SolutionDir, "README.md");
    private void InitNuGetConfig()         => ExtractResource(SolutionDir, "NuGet.config");

    private void ExtractResource(string targetFolder, string fileName)
    {
        Save(Path.Combine(targetFolder, fileName), GetResource(fileName));
    }

    private static string GetResource(string fileName) 
        => GetEmbeddedResourceText(GetExecutingAssembly(), "TestResources", fileName);

    /// <summary>
    /// Deletes the isolated temp folder and all its contents.
    /// </summary>
    internal void Cleanup()
    {
        LogTitle("Clean up");
        try
        {
            if (Directory.Exists(SolutionDir))
            {
                Log($"Deleting temp dir: {SolutionDir}");
                Directory.Delete(SolutionDir, recursive: true);
            }
            // ncrunch: no coverage start
            else
            {
                Log($"Temp dir did not exist: {SolutionDir}");
            }
        }
        catch (Exception ex)
        {
            Log($"Could not delete temp dir: {ex.Message}");
        }
        // ncrunch: no coverage end

        Log(); // Extra for CI
    }

    void IDisposable.Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    ~TestBase() => Cleanup(); // ncrunch: no coverage
}
