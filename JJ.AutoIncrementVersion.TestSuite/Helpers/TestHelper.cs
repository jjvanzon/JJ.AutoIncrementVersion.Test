
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
    private const string PackageVersion = "4.2.5746";
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
        SetCsprojVersion("4.3.0"); // TODO: Should not assume the version will start with 4.3. Should use current value for that major/minor.
        Restore();
    }

    private void ExtractResourceBuildNumXml() => ExtractResource(BuildNumXmlResourceName, BuildNumXmlPath);
    private void ExtractResourceDirectoryBuildProps() => ExtractResource(DirectoryBuildPropsResourceName, DirectoryBuildPropsPath);
    private void ExtractResourceCsproj() => ExtractResource(CsprojResourceName, CsprojPath);
    private void ExtractResourceDummyTxt() => ExtractResource(DummyTxtResourceName, Path.Combine(ProjectDir, "Dummy.txt"));
    private void ExtractResourceReadMe() => ExtractResource(ReadMeResourceName, Path.Combine(SolutionDir, "README.md"));
    private void ExtractResourceNuGetConfig() => ExtractResource(NuGetConfigResourceName, Path.Combine(SolutionDir, "NuGet.config"));

    private static void ExtractResource(string logicalName, string targetPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(logicalName);

        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Embedded resource '{logicalName}' not found. Available: " +
                string.Join(", ", assembly.GetManifestResourceNames()));
        }

        using var reader = new StreamReader(stream);
        WriteAllText(targetPath, reader.ReadToEnd());
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
        Restore(); // JJ added
        Log("Rebuild");
        return RunDotNet($"build \"{CsprojPath}\" -c Release -v:Normal --no-incremental");
    }

    public string RebuildWithArgs(string? extraArgs = null)
    {
        Restore(); // JJ added
        Log($"Rebuild with {extraArgs}");
        return RunDotNet($"build \"{CsprojPath}\" -c Release -v:Normal --no-incremental {extraArgs}");
    }

    public string RebuildDebug()
    {
        Restore(); // JJ added
        Log("Rebuild Debug");
        return RunDotNet($"build \"{CsprojPath}\" -c Debug -v:Normal --no-incremental");
    }

    public void InstallPackage()
    {
        Log("Install package");
        RunDotNet($"add \"{CsprojPath}\" package {PackageId} --version {PackageVersion}");
        // dotnet add package includes its own restore (matching VS NuGet UI behavior).
        // Or maybe not
        Restore(); // JJ added
      }

    public void UninstallPackage()
    {
        Log("Uninstall package");
        RunDotNet($"remove \"{CsprojPath}\" package {PackageId}");
        Restore(); // AI added
    }
    
    private void Restore()
    {
        Log("Restore");
        RunDotNet($"restore \"{CsprojPath}\"");
    }

    private string RunDotNet(string arguments)
    {
        //Log($"> dotnet {arguments}");

        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
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
            throw new TimeoutException($"dotnet {arguments} timed out after {timeoutSeconds}s");
        }

        // .NET may flush async after WaitForExit(int); call the parameterless overload.
        process.WaitForExit();

        var result = new CommandLineResult(process.ExitCode, stdout.ToString(), stderr.ToString());
        //if (result.Output.Length > 0) Log(result.Output.TrimEnd());
        //if (result.Error.Length > 0) Log($"[stderr] {result.Error.TrimEnd()}");

        // TODO: This is still not enough? It could be exit code 0 and no error text? But error in the output?
        bool hasError = result.ExitCode != 0 || !string.IsNullOrWhiteSpace(result.Error);
        if (hasError)
        {
            throw new Exception($"dotnet {arguments} failed: Exit code {result.ExitCode} {result.Error} {result.Output}");
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
}
