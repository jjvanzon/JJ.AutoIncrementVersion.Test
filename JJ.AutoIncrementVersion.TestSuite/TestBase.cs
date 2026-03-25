
namespace JJ.AutoIncrementVersion.TestSuite;

/// <summary>
/// Helpers for running dotnet CLI commands, manipulating project files,
/// and inspecting build output — used by the automated test-plan tests.
/// Logs the actions to Console or Debug output.
/// Each instance creates an isolated copy of the test files under a random
/// temp folder so that tests do not interfere with each other or the repo.
/// </summary>
public class TestBase : IDisposable
{
    // TODO: Should I relate Verbosity to the value this test suite's compiled with?
    /// <summary>
    /// - <c>Diagnostic</c> or <c>Detailed</c> will log all build output.<br/>
    /// - <c>Normal</c> and <c>Minimal</c> will only check silently for errors internally.<br/>
    /// - <c>Quiet</c> won't work, because it'll swallow diagnostics used by the logic.
    /// </summary>
    private const string Verbosity = "Detailed";

    // Paths

    private string SolutionDir { get; }
    private string ProjectDir { get; }
    private string CsprojFilePath { get; }
    private string BuildNumXmlFilePath { get; }
    private string DirPropsFilePath { get; }

    private const string PackageId = "JJ.AutoIncrementVersion";
    private const string TestProjectName = "JJ.AutoIncrementVersion.Test";

    // Init / Cleanup

    public TestBase()
    {
        // Create a random isolated folder in the system temp directory
        // (outside the repo tree, so MSBuild won't pick up the repo's Directory.Build.props).
        //SolutionDir       = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString());
        SolutionDir         = Path.Combine(Path.GetTempPath(), "JJ.AutoIncrementVersion.TestRuns", NewGuid().ToString("N"));
        ProjectDir          = Path.Combine(SolutionDir, TestProjectName);
        CsprojFilePath      = Path.Combine(ProjectDir, $"{TestProjectName}.csproj");
        BuildNumXmlFilePath = Path.Combine(SolutionDir, "BuildNum.xml");
        DirPropsFilePath    = Path.Combine(SolutionDir, "Directory.Build.props");
    }

    /// <summary>
    /// Re-extracts all embedded test files to the isolated folder,
    /// restoring them to their original state (replaces git restore).
    /// </summary>
    public void InitInstalledState()
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
    public void InitUninstalled()
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

    private void InitCsproj             () => ExtractResource(ProjectDir, TestProjectName + ".csproj");
    private void InitDirectoryBuildProps() => ExtractResource(SolutionDir, "Directory.Build.props");
    private void InitBuildNumXml        () => ExtractResource(SolutionDir, "BuildNum.xml");
    private void InitDummyTxt           () => ExtractResource(ProjectDir, "Dummy.txt");
    private void InitReadMe             () => ExtractResource(SolutionDir, "README.md");
    private void InitNuGetConfig        () => ExtractResource(SolutionDir, "NuGet.config");

    private void ExtractResource(string targetFolder, string fileName)
    {
        Save(Path.Combine(targetFolder, fileName), GetResource(fileName));
    }

    private static string GetResource(string fileName) 
        => GetEmbeddedResourceText(GetExecutingAssembly(), "TestResources", fileName);

    /// <summary>
    /// Deletes the isolated temp folder and all its contents.
    /// </summary>
    public void Cleanup()
    {
        #if DEBUG
        return;
        #endif

        // ReSharper disable HeuristicUnreachableCode
        #pragma warning disable CS0162 // Unreachable code

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
            Log($"⚠ Could not delete isolated folder: {ex.Message}");
        }

        // ReSharper restore HeuristicUnreachableCode
        #pragma warning restore CS0162 // Unreachable code
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    ~TestBase() => Cleanup(); // ncrunch: no coverage
  
    // Logging

    /// <summary>
    /// Logs a line to the Debug or Console output.
    /// </summary>
    public void Log(string message = "")
    {
        Trace.WriteLine(message);
        return;

        #if DEBUG
        Debug.WriteLine(message);
        #else
        Console.WriteLine(message);
        #endif
    }
      
    // File Helpers

    public bool   BuildNumXmlExists()              => Exists(BuildNumXmlFilePath);
    public bool   DirPropsExists()                 => Exists(DirPropsFilePath);
    public string ReadBuildNumXml()                => ReadAllText(BuildNumXmlFilePath);
    public string ReadDirProps()                   => ReadAllText(DirPropsFilePath);
    public void   WriteBuildNumXml(string content) => Save(BuildNumXmlFilePath, content);
    public void   WriteDirProps(string content)    => Save(DirPropsFilePath, content);
    public void   DeleteBuildNumXml()              => Delete(BuildNumXmlFilePath);
    public void   DeleteDirProps()                 => Delete(DirPropsFilePath);

    private bool Exists(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        bool exists = File.Exists(filePath);
        if (!exists)
        {
            Log($"{fileName} missing");
            return false;
        }
    
        long length = new FileInfo(filePath).Length;
        // ncrunch: no coverage start
        if (length == 0)
        {
            Log($"{fileName} empty");
            return false;
        }
        // ncrunch: no coverage end

        Log($"{fileName} exists");

        return true;
    }

    private void Save(string filePath, string content)
    {
        string fileName = Path.GetFileName(filePath);
        Log("Save " + fileName);
        WriteAllText(filePath, content);
    }

    private void Delete(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        Log("Deleting " + fileName);
        File.Delete(filePath);
    }

    // Run Processes

    public void RebuildsIncrement(int repeats = 3)
    {
        int prevBuildNum = default;
        for (int i = 0; i < repeats; i++)
        {
            int buildNum = GetBuildNumFromXml();
            string output = Rebuild();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith($".{buildNum}.nupkg"));
            Log();

            if (i != 0)
            {
                IsTrue(buildNum == prevBuildNum + 1);
            }
            prevBuildNum = buildNum;
        }
    }

    public void RebuildExpectFail()
    {
        string output = "";
        try
        {
            output = Rebuild();
        } // ncrunch: no coverage
        catch (Exception ex)
        {
            // The first build may fail because $(BuildNum) resolves to empty.
            // This is expected per the manual plan.
            const string expectedMessage = "is not a valid version string";

            bool hasExpectedError =
                ex.Message.Contains(expectedMessage, OrdinalIgnoreCase) ||
                ex.Message.Contains("NETSDK1018", OrdinalIgnoreCase);

            // ncrunch: no coverage start
            if (!hasExpectedError)
            {
                // TODO: Use InnerException instead?
                //Log(output);
                string text = ex.Message;
                if (!text.Contains(output))
                {
                    text += $"Output: {output}";
                }

                throw new Exception(
                    $"First build failed but not with the expected error '{expectedMessage}'. {text}");
            }
            // ncrunch: no coverage end

            Log("Build failed: 'not a valid version string'");
            return;
        } // ncrunch: no coverage

        Log($"Build succeeded while expecting error: 'not a valid version string'. Output: {output}"); // ncrunch: no coverage
    }

    public string Rebuild()
    {
        Log("Rebuild");
        //return RunProcess("dotnet", $"build \"{CsprojFilePath}\" -c Release -v {Verbosity} --no-incremental --no-restore");
        return RunProcess("dotnet", $"msbuild \"{CsprojFilePath}\" /t:Rebuild /p:Configuration=Release /v:{Verbosity}");
    }

    public string Rebuild(string? extraArgs)
    {
        Log($"Rebuild with {extraArgs}");
        //return RunProcess("dotnet"`, $"build \"{CsprojFilePath}\" -c Release -v {Verbosity} --no-incremental --no-restore {extraArgs}");
        return RunProcess("dotnet", $"msbuild \"{CsprojFilePath}\" /t:Rebuild /p:Configuration=Release /v:{Verbosity} {extraArgs}");
    }

    public string RebuildDebug()
    {
        Log("Rebuild Debug");
        //return RunProcess("dotnet", $"build \"{CsprojFilePath}\" -c Debug -v {Verbosity} --no-incremental --no-restore");
        return RunProcess("dotnet", $"msbuild \"{CsprojFilePath}\" /t:Rebuild /p:Configuration=Debug /v:{Verbosity}");
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

        var outputSB = new StringBuilder();
        var errorSB = new StringBuilder();

        process.OutputDataReceived += (_, e) => outputSB.AppendLine(e.Data ?? "");
        process.ErrorDataReceived += (_, e) => errorSB.AppendLine(e.Data ?? "");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        const int timeoutSeconds = 120;
        // ncrunch: no coverage start
        if (!process.WaitForExit(timeoutSeconds * 1000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{fileName} {arguments} timed out after {timeoutSeconds}s");
        }
        // ncrunch: no coverage end

        // .NET may flush async after WaitForExit(int); call the parameterless overload.
        process.WaitForExit();

        var output = outputSB.ToString().TrimEnd();
        var error = errorSB.ToString().Trim();

        if (string.Equals(Verbosity, "Diagnostic", OrdinalIgnoreCase) ||
            string.Equals(Verbosity, "Detailed", OrdinalIgnoreCase))
        {
            Log($"Exit Code = {process.ExitCode}");
            Log($"Error = {error}");
            Log($"Output = {output}");
        }

        bool hasExitCode = process.ExitCode != 0;
        bool hasErrorText = !IsNullOrWhiteSpace(error);
        bool hasErrorInOutput = output.Contains("[error]");
        bool hasError = hasExitCode  || hasErrorText || hasErrorInOutput;

        if (hasError)
        {
            throw new Exception(
                $"{fileName} {arguments} failed " +
                $"{new { hasExitCode, hasErrorText, hasErrorInOutput }}: " +
                $"Exit code {process.ExitCode} {error} {output}");
        }

        return $"{error} {output}";
    }

    // Inspect/Write Values

    public int GetBuildNumFromXml()
    {
        var doc = XDocument.Load(BuildNumXmlFilePath);
        var elements = doc.Descendants("BuildNum").ToArray();
        AreEqual(1, elements.Length);
        IsNotNull(elements[0]);
        string str = elements[0].Value;
        Log($"BuildNum.xml = {str}");
        var value = int.Parse(str);
        return value;
    }

    public void SetBuildNumInXml(int num)
    {
        Log("Set BuildNum.xml to " + num);
        var doc = XDocument.Load(BuildNumXmlFilePath);
        var el = doc.Descendants("BuildNum").First();
        el.Value = num.ToString();
        // Write back as single-line XML (matching original format)
        Save(BuildNumXmlFilePath, doc.Declaration?.ToString() ?? "");
        using var writer = new System.Xml.XmlTextWriter(BuildNumXmlFilePath, Encoding.UTF8);
        writer.Formatting = System.Xml.Formatting.None;
        doc.WriteTo(writer);
    }

    
    /// <summary>
    /// Replaces the <c>&lt;Version&gt;</c> value in the csproj.
    /// </summary>
    private void SetCsprojVersion(string version)
    {
        Log($"Set ver = {version}");
        string text = ReadAllText(CsprojFilePath);
        text = Regex.Replace(text, @"<Version>[^<]*</Version>", $"<Version>{version}</Version>");
        Save(CsprojFilePath, text);
    }

    /// <summary>
    /// Extracts major.minor from the csproj Version element.
    /// E.g. "4.3.0" → "4.3", "4.3.$(BuildNum)" → "4.3"
    /// </summary>
    private string GetCsprojMajorMinor()
    {
        string text = ReadAllText(CsprojFilePath);
        Match versionMatch = Match(text, @"<Version>\s*(\d+\.\d+)", IgnoreCase);
        
        // ncrunch: no coverage start
        if (!versionMatch.Success)
        {
            throw new InvalidOperationException("Could not extract major.minor from csproj Version element.");
        }
        // ncrunch: no coverage end

        return versionMatch.Groups[1].Value;
    }

    /// <summary>
    /// Sets csproj Version to &lt;major&gt;.&lt;minor&gt;.0,
    /// extracting major.minor from the current csproj Version value.
    /// </summary>
    public void SetProjPatchNum(string patch)
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
        Log("Remove package reference");
        string text = ReadAllText(CsprojFilePath);
        const string pattern = @"\s*<PackageReference\s+Include=""JJ\.AutoIncrementVersion""[^/]*/>\s*";
        text = Regex.Replace(text, pattern, "\n");
        Save(CsprojFilePath, text);
    }

    /// <summary>
    /// Extracts the nupkg file name (e.g. "JJ.AutoIncrementVersion.Test.4.3.5.nupkg")
    /// from build output.
    /// </summary>
    public string ExtractPackageFileName(string output)
    {
        var match = Match(output, @"(JJ\.AutoIncrementVersion\.Test\.\S+\.nupkg)");
        var packageFileName = match.Success ? match.Groups[1].Value : null;

        // ncrunch: no coverage start
        if (IsNullOrWhiteSpace(packageFileName))
        {
            throw new Exception($"Package '{TestProjectName}*.nupkg' not found in output: " + output);
        }
        // ncrunch: no coverage end

        Log($"Package name = {packageFileName}");

        return packageFileName;
    }

    /// <summary>
    /// Ensures Directory.Build.props imports BuildNum.xml only for Release configuration.
    /// If the Release condition is missing, it is inserted.
    /// </summary>
    public void EnsureDirPropsReleaseCondition()
    {
        string content = ReadDirProps();

        // ncrunch: no coverage start
        if (content.Contains("$(Configuration)=='Release'", OrdinalIgnoreCase))
        {
            Log("Directory.Build.props contains condition: $(Configuration)=='Release'");
            return;
        }
        // ncrunch: no coverage end

        Log("Adding condition to Directory.Build.props: $(Configuration)=='Release'");

        const string pattern = "Condition\\s*=\\s*\"Exists\\('BuildNum\\.xml'\\)\"";
        const string replacement = "Condition=\"Exists('BuildNum.xml') And $(Configuration)=='Release'\"";

        string updated = Replace(content, pattern, replacement, IgnoreCase);

        // ncrunch: no coverage start
        if (updated == content)
        {
            throw new InvalidOperationException("Could not inject Release condition into Directory.Build.props.");
        }
        // ncrunch: no coverage end

        WriteDirProps(updated);
    }

    private string GetEmbeddedPackageVersion()
    {
        string content = GetResource(TestProjectName + ".csproj");

        Match match = Match(content, @"<PackageReference\s+Include=""JJ\.AutoIncrementVersion""\s+Version=""([^""]+)""", IgnoreCase);

        // ncrunch: no coverage start
        if (!match.Success)
        {
            throw new InvalidOperationException(
                "Could not extract JJ.AutoIncrementVersion package version from embedded csproj.");
        }
        // ncrunch: no coverage end

        var packageVersion = match.Groups[1].Value;

        Log($"Package version = {packageVersion}");

        return packageVersion;
    }
}
