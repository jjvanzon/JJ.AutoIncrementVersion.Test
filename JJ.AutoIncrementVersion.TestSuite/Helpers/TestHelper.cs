using static System.Reflection.Assembly;
using static JJ.Framework.Common.Legacy.EmbeddedResourceHelper;

namespace JJ.AutoIncrementVersion.TestSuite.Helpers;

/// <summary>
/// Helpers for running dotnet CLI commands, manipulating project files,
/// and inspecting build output — used by the automated test-plan tests.
/// Each instance creates an isolated copy of the test files under a random
/// temp folder so that tests do not interfere with each other or the repo.
/// </summary>
internal sealed class TestHelper : IDisposable
{
    // Paths

    private string SolutionDir { get; }
    private string ProjectDir { get; }
    private string CsprojPath { get; }
    private string BuildNumXmlPath { get; }
    private string DirectoryBuildPropsPath { get; }

    private const string PackageId = "JJ.AutoIncrementVersion";
    // TODO: Fixed version number not good. Latest from Pre-Release-Package-Feed is better. Kinda bad, because if we keep this, we'd assume the tests test the latest, which it never would.
    private const string TestProjectName = "JJ.AutoIncrementVersion.Test";

    // Embedded resource logical names
    private const string CsprojResourceName = "TestFiles.JJ.AutoIncrementVersion.Test.csproj";
    private const string DirectoryBuildPropsResourceName = "TestFiles.Directory.Build.props";
    private const string BuildNumXmlResourceName = "TestFiles.BuildNum.xml";
    private const string ReadMeResourceName = "TestFiles.README.md";
    private const string DummyTxtResourceName = "TestFiles.Dummy.txt";
    private const string NuGetConfigResourceName = "TestFiles.NuGet.config";

    // File Helpers

    public bool BuildNumXmlExists() => File.Exists(BuildNumXmlPath);
    public bool DirectoryBuildPropsExists() => File.Exists(DirectoryBuildPropsPath);
    public string ReadBuildNumXml() => ReadAllText(BuildNumXmlPath);
    public string ReadDirectoryBuildProps() => ReadAllText(DirectoryBuildPropsPath);
    public void WriteBuildNumXml(string content) => WriteAllText(BuildNumXmlPath, content);
    public void WriteDirectoryBuildProps(string content) => WriteAllText(DirectoryBuildPropsPath, content);
    public void DeleteBuildNumXml() => File.Delete(BuildNumXmlPath);
    public void DeleteDirectoryBuildProps() => File.Delete(DirectoryBuildPropsPath);

    // Init / Cleanup

    public TestHelper()
    {
        // Create a random isolated folder in the system temp directory
        // (outside the repo tree, so MSBuild won't pick up the repo's Directory.Build.props).
        string basePath = Path.Combine(Path.GetTempPath(), "JJ.AutoIncrementVersion.TestRuns", Guid.NewGuid().ToString("N"));
        //string basePath = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString());
        SolutionDir = basePath;
        ProjectDir = Path.Combine(SolutionDir, TestProjectName);
        CsprojPath = Path.Combine(ProjectDir, $"{TestProjectName}.csproj");
        BuildNumXmlPath = Path.Combine(SolutionDir, "BuildNum.xml");
        DirectoryBuildPropsPath = Path.Combine(SolutionDir, "Directory.Build.props");
    }

    /// <summary>
    /// Re-extracts all embedded test files to the isolated folder,
    /// restoring them to their original state (replaces git restore).
    /// </summary>
    public void SetInstalledState()
    {
        Log("Set installed state");
        CreateDirectory(SolutionDir);
        CreateDirectory(ProjectDir);
        ExtractResourceBuildNumXml();
        ExtractResourceDirectoryBuildProps();
        ExtractResourceCsproj();
        ExtractResourceDummyTxt();
        ExtractResourceReadMe();
        ExtractResourceNuGetConfig();
        Restore();
    }

    /// <summary>
    /// Full "Set Initial State" as described in the manual test plan:
    /// uninstall package, delete BuildNum.xml &amp; Directory.Build.props,
    /// replace $(BuildNum) with 0 in csproj.
    /// </summary>
    public void SetUninstalledState()
    {
        Log("Set uninstalled state");
        CreateDirectory(SolutionDir);
        CreateDirectory(ProjectDir);
        ExtractResourceCsproj();
        ExtractResourceDummyTxt();
        ExtractResourceReadMe();
        ExtractResourceNuGetConfig();
        RemovePackageReferenceFromCsproj();
        SetCsProjPatchNum("0");
        Restore();
    }

    private void ExtractResourceBuildNumXml() => ExtractResource("BuildNum.xml", BuildNumXmlPath);
    private void ExtractResourceDirectoryBuildProps() => ExtractResource("Directory.Build.props", DirectoryBuildPropsPath);
    private void ExtractResourceCsproj() => ExtractResource("JJ.AutoIncrementVersion.Test.csproj", CsprojPath);
    private void ExtractResourceDummyTxt() => ExtractResource("Dummy.txt", Path.Combine(ProjectDir, "Dummy.txt"));
    private void ExtractResourceReadMe() => ExtractResource("README.md", Path.Combine(SolutionDir, "README.md"));
    private void ExtractResourceNuGetConfig() => ExtractResource("NuGet.config", Path.Combine(SolutionDir, "NuGet.config"));

    private static void ExtractResource(string resourceName, string targetPath)
    {
        var text = GetTestResource(resourceName);
        WriteAllText(targetPath, text);
    }

    private static string GetTestResource(string resourceName)
    {
        string text = GetEmbeddedResourceText(GetExecutingAssembly(), "TestResources", resourceName);
        return text;
    }

    /// <summary>
    /// Deletes the isolated temp folder and all its contents.
    /// </summary>
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(SolutionDir))
            {
                Delete(SolutionDir, recursive: true);
            }
        }
        catch (Exception ex)
        {
            LogWarning($"Could not delete isolated folder: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    ~TestHelper() => Cleanup();

    // Logging

    public void Log(string message)
    {
        #if DEBUG
        Debug.WriteLine(message);
        #else
        WriteLine(message);
        #endif
    }

    public void LogStep(string step) => Log($"── STEP: {step}");
    public void LogResult(string result) => Log($"   ✓ {result}");
    public void LogWarning(string warning) => Log($"   ⚠ {warning}");

    // Run Processes

    public string Rebuild()
    {
        Log("Rebuild");
        return RunProcess("dotnet", $"msbuild \"{CsprojPath}\" /t:Rebuild /p:Configuration=Release /v:Normal");
    }

    public string RebuildWithArgs(string? extraArgs = null)
    {
        Log($"Rebuild with {extraArgs}");
        return RunProcess("dotnet", $"msbuild \"{CsprojPath}\" /t:Rebuild /p:Configuration=Release /v:Normal {extraArgs}");
    }

    public string RebuildDebug()
    {
        Log("Rebuild Debug");
        return RunProcess("dotnet", $"msbuild \"{CsprojPath}\" /t:Rebuild /p:Configuration=Debug /v:Normal");
    }

    public void InstallPackage()
    {
        Log("Install package");
        string version = GetEmbeddedPackageVersion();
        RunProcess("dotnet", $"add \"{CsprojPath}\" package {PackageId} --version {version}");
        Restore();
      }

    public void UninstallPackage()
    {
        Log("Uninstall package");
        RunProcess("dotnet", $"remove \"{CsprojPath}\" package {PackageId}");
        Restore(); // Or uninstall isn't finalized somehow.
    }
    
    private void Restore()
    {
        Log("Restore");
        RunProcess("dotnet", $"restore \"{CsprojPath}\"");
    }

    private string RunProcess(string fileName, string arguments)
    {
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = ProjectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { stdout.AppendLine(e.Data ?? ""); };
        process.ErrorDataReceived += (_, e) => { stderr.AppendLine(e.Data ?? ""); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        const int timeoutSeconds = 120;
        if (!process.WaitForExit(timeoutSeconds * 1000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{fileName} {arguments} timed out after {timeoutSeconds}s");
        }

        // .NET may flush async after WaitForExit(int); call the parameterless overload.
        process.WaitForExit();

        var result = new CommandLineResult(process.ExitCode, stdout.ToString(), stderr.ToString());

        // TODO: This is still not enough? Build might have exit code 0 and no error text? But error in the output?
        // TODO: Suspicious code line. Restore/install/uninstall/build results may block/continue behavior on varying conditions.
        bool hasError = result.ExitCode != 0 || !string.IsNullOrWhiteSpace(result.Error);
        if (hasError)
        {
            throw new Exception($"{fileName} {arguments} failed: Exit code {result.ExitCode} {result.Error} {result.Output}");
        }

        return result.Output;
    }

    // Inspect/Write Values

    public int GetBuildNumFromXml()
    {
        var doc = XDocument.Load(BuildNumXmlPath);
        string? val = doc.Descendants("BuildNum").FirstOrDefault()?.Value;
        return int.Parse(val ?? "0");
    }

    public void SetBuildNumInXml(int num)
    {
        Log("Set BuildNum.xml to " + num);
        var doc = XDocument.Load(BuildNumXmlPath);
        var el = doc.Descendants("BuildNum").First();
        el.Value = num.ToString();
        // Write back as single-line XML (matching original format)
        WriteAllText(BuildNumXmlPath, doc.Declaration?.ToString() ?? "");
        using var writer = new System.Xml.XmlTextWriter(BuildNumXmlPath, Encoding.UTF8);
        writer.Formatting = System.Xml.Formatting.None;
        doc.WriteTo(writer);
    }

    /// <summary>
    /// Replaces the <c>&lt;Version&gt;</c> value in the csproj.
    /// </summary>
    public void SetCsprojVersion(string version)
    {
        string text = ReadAllText(CsprojPath);
        text = Regex.Replace(text, @"<Version>[^<]*</Version>", $"<Version>{version}</Version>");
        WriteAllText(CsprojPath, text);
    }

    /// <summary>
    /// Extracts major.minor from the csproj Version element.
    /// E.g. "4.3.0" → "4.3", "4.3.$(BuildNum)" → "4.3"
    /// </summary>
    private string GetCsprojMajorMinor()
    {
        string text = ReadAllText(CsprojPath);
        Match versionMatch = Regex.Match(text, @"<Version>\s*(\d+\.\d+)", IgnoreCase);
        
        if (!versionMatch.Success)
        {
            throw new InvalidOperationException("Could not extract major.minor from csproj Version element.");
        }

        return versionMatch.Groups[1].Value;
    }

    /// <summary>
    /// Sets csproj Version to &lt;major&gt;.&lt;minor&gt;.0,
    /// extracting major.minor from the current csproj Version value.
    /// </summary>
    public void SetCsProjPatchNum(string patch)
    {
        string majorMinor = GetCsprojMajorMinor();
        SetCsprojVersion($"{majorMinor}.{patch}");
    }

    /// <summary>
    /// Checks whether the csproj currently references the package.
    /// </summary>
    public bool CsprojHasPackageReference()
    {
        string text = ReadAllText(CsprojPath);
        return text.Contains($"Include=\"{PackageId}\"", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Removes the JJ.AutoIncrementVersion PackageReference from the csproj
    /// by editing the file directly (no dotnet CLI needed).
    /// </summary>
    public void RemovePackageReferenceFromCsproj()
    {
        string text = ReadAllText(CsprojPath);
        // Remove the <PackageReference Include="JJ.AutoIncrementVersion" ... /> line
        const string pattern = @"\s*<PackageReference\s+Include=""JJ\.AutoIncrementVersion""[^/]*/>\s*";
        text = Regex.Replace(text, pattern, "\n");
        WriteAllText(CsprojPath, text);
    }

    /// <summary>
    /// Extracts the nupkg file name (e.g. "JJ.AutoIncrementVersion.Test.4.3.5.nupkg")
    /// from build output.
    /// </summary>
    public string? ExtractNupkgName(string output)
    {
        var match = Regex.Match(output, @"(JJ\.AutoIncrementVersion\.Test\.\S+\.nupkg)");
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Extracts the last segment of the version from the nupkg name.
    /// E.g. "JJ.AutoIncrementVersion.Test.4.3.7.nupkg" → 7
    /// </summary>
    public int? ExtractBuildNumFromNupkgName(string output)
    {
        var match = Regex.Match(output, @"JJ\.AutoIncrementVersion\.Test\.[\d]+\.[\d]+\.([\d]+)\.nupkg");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    public bool OutputContainsNupkgEndingWith(string output, string suffix)
    {
        string? name = ExtractNupkgName(output);
        return name is not null && name.EndsWith(suffix, OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures Directory.Build.props imports BuildNum.xml only for Release configuration.
    /// If the Release condition is missing, it is inserted.
    /// </summary>
    public void EnsureDirectoryBuildPropsHasReleaseCondition()
    {
        string content = ReadDirectoryBuildProps();

        if (content.Contains("$(Configuration)=='Release'", OrdinalIgnoreCase))
        {
            return;
        }

        const string pattern = "Condition\\s*=\\s*\"Exists\\('BuildNum\\.xml'\\)\"";
        const string replacement = "Condition=\"Exists('BuildNum.xml') And $(Configuration)=='Release'\"";

        string updated = Regex.Replace(content, pattern, replacement, IgnoreCase);

        if (updated == content)
        {
            throw new InvalidOperationException("Could not inject Release condition into Directory.Build.props.");
        }

        WriteDirectoryBuildProps(updated);
    }

    private string GetEmbeddedPackageVersion()
    {
        string content = GetTestResource("JJ.AutoIncrementVersion.Test.csproj");

        Match match = Regex.Match(content, @"<PackageReference\s+Include=""JJ\.AutoIncrementVersion""\s+Version=""([^""]+)""", IgnoreCase);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                "Could not extract JJ.AutoIncrementVersion package version from embedded csproj.");
        }

        var packageVersion = match.Groups[1].Value;
        return packageVersion;
    }
}
