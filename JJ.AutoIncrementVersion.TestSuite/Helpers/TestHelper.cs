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
    private const string ResCsproj = "TestFiles.JJ.AutoIncrementVersion.Test.csproj";
    private const string ResDirectoryBuildProps = "TestFiles.Directory.Build.props";
    private const string ResBuildNumXml = "TestFiles.BuildNum.xml";
    private const string ResReadme = "TestFiles.README.md";
    private const string ResDummyTxt = "TestFiles.Dummy.txt";

    // File Helpers

    public bool BuildNumXmlExists() => Exists(BuildNumXmlPath);
    public bool DirectoryBuildPropsExists() => Exists(DirectoryBuildPropsPath);
    public string ReadBuildNumXml() => ReadAllText(BuildNumXmlPath);
    public string ReadDirectoryBuildProps() => ReadAllText(DirectoryBuildPropsPath);
    public void WriteBuildNumXml(string content) => WriteAllText(BuildNumXmlPath, content);
    public void WriteDirectoryBuildProps(string content) => WriteAllText(DirectoryBuildPropsPath, content);
    public void DeleteBuildNumXml() => Delete(BuildNumXmlPath);
    public void DeleteDirectoryBuildProps() => Delete(DirectoryBuildPropsPath);

    // Constructor / Embedded Resource Extraction

    public TestHelper()
    {
        // Create a random isolated folder in the system temp directory
        // (outside the repo tree, so MSBuild won't pick up the repo's Directory.Build.props).
        string basePath = Path.Combine(Path.GetTempPath(), "JJ.AutoIncrementVersion.TestRuns", Guid.NewGuid().ToString("N"));
        SolutionDir = basePath;
        ProjectDir = Path.Combine(SolutionDir, TestProjectName);
        CsprojPath = Path.Combine(ProjectDir, $"{TestProjectName}.csproj");
        BuildNumXmlPath = Path.Combine(SolutionDir, "BuildNum.xml");
        DirectoryBuildPropsPath = Path.Combine(SolutionDir, "Directory.Build.props");

        Directory.CreateDirectory(SolutionDir);
        Directory.CreateDirectory(ProjectDir);

        // Extract all embedded test files to the isolated folder.
        ExtractAllResources();

        Log($"── TestHelper: isolated folder created at {SolutionDir}");
    }

    private void ExtractAllResources()
    {
        ExtractResource(ResBuildNumXml, BuildNumXmlPath);
        ExtractResource(ResDirectoryBuildProps, DirectoryBuildPropsPath);
        ExtractResource(ResCsproj, CsprojPath);
        ExtractResource(ResDummyTxt, Path.Combine(ProjectDir, "Dummy.txt"));
        ExtractResource(ResReadme, Path.Combine(SolutionDir, "README.md"));
    }

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

    public CommandLineResult RunDotnet(string arguments)
    {
        Log($"> dotnet {arguments}");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = ProjectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        // REVIEWED: Cool. Didn't know before how to capture output. Bit verbose from .NET, but nice that it's possible
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
        if (result.Output.Length > 0) Log(result.Output.TrimEnd());
        if (result.Error.Length > 0) Log($"[stderr] {result.Error.TrimEnd()}");

        /*
        if (mustThrow)
        {
            bool hasError = result.ExitCode != 0 || !string.IsNullOrWhiteSpace(result.Error);
            if (hasError)
            {
                string errorText = result.Error;
                if (string.IsNullOrWhiteSpace(errorText))
                {
                    errorText = result.Output;
                }

                throw new Exception($"dotnet {arguments} failed: Exit code {result.ExitCode}" + errorText);
            }
        }
        */

        return result;
    }

    public CommandLineResult Build(string configuration = "Release")
    {
        string args = $"build \"{CsprojPath}\" -c {configuration}";
        return RunDotnet(args);
    }

    public CommandLineResult BuildWithArgs(string configuration = "Release", string? extraArgs = null)
    {
        string args = $"build \"{CsprojPath}\" -c {configuration}";
        if (extraArgs is not null) args += $" {extraArgs}";
        return RunDotnet(args);
    }

    public CommandLineResult Rebuild(string configuration = "Release")
        => BuildWithArgs(configuration, "--no-incremental");

    public CommandLineResult InstallPackage()
        => RunDotnet($"add \"{CsprojPath}\" package {PackageId} --version {PackageVersion}");

    public CommandLineResult UninstallPackage()
        => RunDotnet($"remove \"{CsprojPath}\" package {PackageId}");

    // Inspect/Write Values

    public int GetBuildNumFromXml()
    {
        var doc = XDocument.Load(BuildNumXmlPath);
        string? val = doc.Descendants("BuildNum").FirstOrDefault()?.Value;
        return int.Parse(val ?? "0");
    }

    public void SetBuildNumInXml(int num)
    {
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
        Log($"   Set <Version> to {version}");
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
        Log($"   Removed {PackageId} PackageReference from csproj");
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
    public int? ExtractBuildNumFromNupkg(string output)
    {
        var match = Regex.Match(output, @"JJ\.AutoIncrementVersion\.Test\.[\d]+\.[\d]+\.([\d]+)\.nupkg");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    public bool OutputContainsNupkgEndingWith(string output, string suffix)
    {
        string? name = ExtractNupkgName(output);
        return name is not null && name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    // Init / Cleanup

    /// <summary>
    /// Re-extracts all embedded test files to the isolated folder,
    /// restoring them to their original state (replaces git restore).
    /// </summary>
    public void RestoreAll()
    {
        Log("   Restoring isolated files from embedded resources...");
        ExtractAllResources();
        LogResult("Isolated files restored to original state");
    }

    /// <summary>
    /// Full "Set Initial State" as described in the manual test plan:
    /// uninstall package, delete BuildNum.xml &amp; Directory.Build.props,
    /// replace $(BuildNum) with 0 in csproj.
    /// </summary>
    public void SetInitialState()
    {
        LogStep("Set Initial State");
        ExtractAllResources(); // Re-extract the files to a known baseline first.
        RemovePackageReferenceFromCsproj();
        DeleteBuildNumXml();
        DeleteDirectoryBuildProps();
        SetCsprojVersion("4.3.0"); // TODO: Should not assume the version will start with 4.3. Should use current value for that major/minor.
        LogResult("Initial state set (package removed, xml/props deleted, version=4.3.0)");
    }

    /// <summary>
    /// Deletes the isolated temp folder and all its contents.
    /// </summary>
    public void Cleanup()
    {
        Log("── CLEANUP ──");
        try
        {
            if (Directory.Exists(SolutionDir))
            {
                Directory.Delete(SolutionDir, recursive: true);
                LogResult($"Deleted isolated folder: {SolutionDir}");
            }
        }
        catch (Exception ex)
        {
            LogWarning($"Could not delete isolated folder: {ex.Message}");
        }
    }

    public void Dispose() => Cleanup();
}
