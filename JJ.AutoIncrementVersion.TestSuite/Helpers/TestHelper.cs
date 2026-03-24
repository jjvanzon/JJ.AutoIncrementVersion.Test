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
    private string CsprojFilePath { get; }
    private string BuildNumXmlFilePath { get; }
    private string DirPropsFilePath { get; }
    private string DummyTxtFilePath { get; }
    private string ReadmeMDFilePath { get; }
    private string NuGetConfigFilePath { get; }

    private const string PackageId = "JJ.AutoIncrementVersion";
    private const string TestProjectName = "JJ.AutoIncrementVersion.Test";

    // Init / Cleanup

    public TestHelper()
    {
        // Create a random isolated folder in the system temp directory
        // (outside the repo tree, so MSBuild won't pick up the repo's Directory.Build.props).
        //SolutionDir       = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString());
        SolutionDir         = Path.Combine(Path.GetTempPath(), "JJ.AutoIncrementVersion.TestRuns", Guid.NewGuid().ToString("N"));
        ProjectDir          = Path.Combine(SolutionDir, TestProjectName);
        CsprojFilePath      = Path.Combine(ProjectDir, $"{TestProjectName}.csproj");
        BuildNumXmlFilePath = Path.Combine(SolutionDir, "BuildNum.xml");
        DirPropsFilePath    = Path.Combine(SolutionDir, "Directory.Build.props");
        DummyTxtFilePath    = Path.Combine(ProjectDir, "Dummy.txt");
        ReadmeMDFilePath    = Path.Combine(SolutionDir, "README.md");
        NuGetConfigFilePath = Path.Combine(SolutionDir, "NuGet.config");
    }

    /// <summary>
    /// Re-extracts all embedded test files to the isolated folder,
    /// restoring them to their original state (replaces git restore).
    /// </summary>
    public void InitInstalledState()
    {
        Log("Init installed state");
        Directory.CreateDirectory(SolutionDir);
        Directory.CreateDirectory(ProjectDir);
        InitCsproj();
        InitBuildNumXml();
        InitDirectoryBuildProps();
        InitDummyTxt();
        InitReadMe();
        InitNuGetConfig();
        Restore();
    }

    /// <summary>
    /// Full "Set Initial State" as described in the manual test plan:
    /// uninstall package, delete BuildNum.xml &amp; Directory.Build.props,
    /// replace $(BuildNum) with 0 in csproj.
    /// </summary>
    public void InitUninstalled()
    {
        Log("Init uninstalled");
        Directory.CreateDirectory(SolutionDir);
        Directory.CreateDirectory(ProjectDir);
        InitCsproj();
        InitDummyTxt();
        InitReadMe();
        InitNuGetConfig();
        RemovePackageReferenceFromCsproj();
        SetCsProjPatchNum("0");
        Restore();
    }

    private void InitCsproj             () => ExtractResource(ProjectDir, TestProjectName + ".csproj");
    private void InitDirectoryBuildProps() => ExtractResource(SolutionDir, "Directory.Build.props");
    private void InitBuildNumXml        () => ExtractResource(SolutionDir, "BuildNum.xml");
    private void InitDummyTxt           () => ExtractResource(ProjectDir, "Dummy.txt");
    private void InitReadMe             () => ExtractResource(SolutionDir, "README.md");
    private void InitNuGetConfig        () => ExtractResource(SolutionDir, "NuGet.config");

    private void ExtractResource(string targetFolder, string fileName)
    {
        Log($"Init file: {fileName} => {targetFolder}");
        WriteAllText(Path.Combine(targetFolder, fileName), GetResource(fileName));
    }

    private static string GetResource(string fileName) 
        => GetEmbeddedResourceText(GetExecutingAssembly(), "TestResources", fileName);

    /// <summary>
    /// Deletes the isolated temp folder and all its contents.
    /// </summary>
    public void Cleanup()
    {
        Log("Clean up");
        try
        {
            if (Directory.Exists(SolutionDir))
            {
                Directory.Delete(SolutionDir, recursive: true);
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
      
    // File Helpers

    public bool   BuildNumXmlExists()                      => Exists(BuildNumXmlFilePath);
    public bool   DirectoryBuildPropsExists()              => Exists(DirPropsFilePath);
    public string ReadBuildNumXml()                        => ReadAllText(BuildNumXmlFilePath);
    public string ReadDirectoryBuildProps()                => ReadAllText(DirPropsFilePath);
    public void   WriteBuildNumXml(string content)         => WriteAllText(BuildNumXmlFilePath, content);
    public void   WriteDirectoryBuildProps(string content) => WriteAllText(DirPropsFilePath, content);
    public void   DeleteBuildNumXml()                      => Delete(BuildNumXmlFilePath);
    public void   DeleteDirectoryBuildProps()              => Delete(DirPropsFilePath);

    private bool Exists(string filePath)
    {
        bool exists = File.Exists(filePath);
        if (!exists)
        {
            return false;
        }
    
        long length = new FileInfo(filePath).Length;
        if (length == 0)
        {
            return false;
        }

        string fileName = Path.GetFileName(filePath);
        Log($"File exists: {fileName} ({filePath})");

        return true;
    }

    // Run Processes

    public void RebuildExpectingInvalidVersion()
    {
        try
        {
            Rebuild();
        }
        catch (Exception ex)
        {
            // TODO: Log

            // The first build may fail because $(BuildNum) resolves to empty.
            // This is expected per the manual plan.
            const string expectedMessage = "is not a valid version string";

            bool hasExpectedError =
                ex.Message.Contains(expectedMessage, OrdinalIgnoreCase) ||
                ex.Message.Contains("NETSDK1018", OrdinalIgnoreCase);

            IsTrue(hasExpectedError, $"First build failed but not with the expected '{expectedMessage}' error.");

            if (hasExpectedError)
            {
                Log("Build failed with error: 'not a valid version string'");
                return;
            }
        }

        Log("Build succeeded while expecting error: 'not a valid version string'.");
    }

    public string Rebuild()
    {
        Log("Rebuild");
        return RunProcess("dotnet", $"msbuild \"{CsprojFilePath}\" /t:Rebuild /p:Configuration=Release /v:Normal");
    }

    public string Rebuild(string? extraArgs)
    {
        Log($"Rebuild with {extraArgs}");
        return RunProcess("dotnet", $"msbuild \"{CsprojFilePath}\" /t:Rebuild /p:Configuration=Release /v:Normal {extraArgs}");
    }

    public string RebuildDebug()
    {
        Log("Rebuild Debug");
        return RunProcess("dotnet", $"msbuild \"{CsprojFilePath}\" /t:Rebuild /p:Configuration=Debug /v:Normal");
    }

    public void InstallPackage()
    {
        Log("Install package");
        string version = GetEmbeddedPackageVersion();
        RunProcess("dotnet", $"add \"{CsprojFilePath}\" package {PackageId} --version {version}");
        Restore();
      }

    public void UninstallPackage()
    {
        Log("Uninstall package");
        RunProcess("dotnet", $"remove \"{CsprojFilePath}\" package {PackageId}");
        Restore(); // Or uninstall isn't finalized somehow.
    }
    
    private void Restore()
    {
        Log("Restore");
        RunProcess("dotnet", $"restore \"{CsprojFilePath}\"");
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
        var doc = XDocument.Load(BuildNumXmlFilePath);
        string? val = doc.Descendants("BuildNum").FirstOrDefault()?.Value;
        return int.Parse(val ?? "0");
    }

    public void SetBuildNumInXml(int num)
    {
        Log("Set BuildNum.xml to " + num);
        var doc = XDocument.Load(BuildNumXmlFilePath);
        var el = doc.Descendants("BuildNum").First();
        el.Value = num.ToString();
        // Write back as single-line XML (matching original format)
        WriteAllText(BuildNumXmlFilePath, doc.Declaration?.ToString() ?? "");
        using var writer = new System.Xml.XmlTextWriter(BuildNumXmlFilePath, Encoding.UTF8);
        writer.Formatting = System.Xml.Formatting.None;
        doc.WriteTo(writer);
    }

    /// <summary>
    /// Replaces the <c>&lt;Version&gt;</c> value in the csproj.
    /// </summary>
    public void SetCsprojVersion(string version)
    {
        Log($"Set .csproj Version = {version}");
        string text = ReadAllText(CsprojFilePath);
        text = Regex.Replace(text, @"<Version>[^<]*</Version>", $"<Version>{version}</Version>");
        WriteAllText(CsprojFilePath, text);
    }

    /// <summary>
    /// Extracts major.minor from the csproj Version element.
    /// E.g. "4.3.0" → "4.3", "4.3.$(BuildNum)" → "4.3"
    /// </summary>
    private string GetCsprojMajorMinor()
    {
        string text = ReadAllText(CsprojFilePath);
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
        string text = ReadAllText(CsprojFilePath);
        return text.Contains($"Include=\"{PackageId}\"", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Removes the JJ.AutoIncrementVersion PackageReference from the csproj
    /// by editing the file directly (no dotnet CLI needed).
    /// </summary>
    public void RemovePackageReferenceFromCsproj()
    {
        Log($"Remove package reference");
        string text = ReadAllText(CsprojFilePath);
        const string pattern = @"\s*<PackageReference\s+Include=""JJ\.AutoIncrementVersion""[^/]*/>\s*";
        text = Regex.Replace(text, pattern, "\n");
        WriteAllText(CsprojFilePath, text);
    }

    /// <summary>
    /// Extracts the nupkg file name (e.g. "JJ.AutoIncrementVersion.Test.4.3.5.nupkg")
    /// from build output.
    /// </summary>
    public string ExtractPackageFileName(string output)
    {
        var match = Regex.Match(output, @"(JJ\.AutoIncrementVersion\.Test\.\S+\.nupkg)");
        var packageFileName = match.Success ? match.Groups[1].Value : null;

        if (string.IsNullOrWhiteSpace(packageFileName))
        {
            throw new Exception($"Package file name '{TestProjectName}*.nupkg' not found in output: " + output);
        }

        Log($"Package file name = {packageFileName}");

        return packageFileName;
    }

    /// <summary>
    /// Extracts the last segment of the version from the nupkg name.
    /// E.g. "JJ.AutoIncrementVersion.Test.4.3.7.nupkg" → 7
    /// </summary>
    public int ExtractPackageBuildNum(string output)
    {
        var match = Regex.Match(output, @"JJ\.AutoIncrementVersion\.Test\.[\d]+\.[\d]+\.([\d]+)\.nupkg");
        if (!match.Success)
        {
            throw new InvalidOperationException(
                $"Could not extract BuildNum from " +
                $"JJ.AutoIncrementVersion.Test build: {output}");
        }

        int buildNum = int.Parse(match.Groups[1].Value);
        
        Log($"Output BuildNum = {buildNum}");

        return buildNum;
    }

    public bool OutputContainsNupkgEndingWith(string output, string suffix)
    {
        string? name = ExtractPackageFileName(output);
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
        string content = GetResource(TestProjectName + ".csproj");

        Match match = Regex.Match(content, @"<PackageReference\s+Include=""JJ\.AutoIncrementVersion""\s+Version=""([^""]+)""", IgnoreCase);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                "Could not extract JJ.AutoIncrementVersion package version from embedded csproj.");
        }

        var packageVersion = match.Groups[1].Value;

        Log($"Package version = {packageVersion}");

        return packageVersion;
    }
}
