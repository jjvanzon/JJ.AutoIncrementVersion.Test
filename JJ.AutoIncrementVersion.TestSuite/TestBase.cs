namespace JJ.AutoIncrementVersion.TestSuite;

/// <summary>
/// Helpers for running dotnet CLI commands, manipulating project files,
/// and inspecting build output — used by the automated test-plan tests.
/// Logs the actions to Console or Debug output.
/// Each instance creates an isolated copy of the test files under a random
/// temp folder so that tests do not interfere with each other or the repo.
/// </summary>
public abstract class TestBase : IDisposable
{
    /// <summary>
    /// <para>Verbosity is passed to the build processes executed in this helper.</para>
    /// Plus:<br/>
    /// - <c>Diagnostic</c> or <c>Detailed</c> will log all build output.<br/>
    /// - <c>Normal</c> and <c>Minimal</c> will only check silently for errors internally.<br/>
    /// - <c>Quiet</c> won't work, because it'll swallow diagnostics used by the logic.
    /// </summary>
    private const string VERBOSITY = Verbosities.Minimal;

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
    private const string TestProjectName = "JJ.AutoIncrementVersion.Test";

    // Init / Cleanup

    internal TestBase()
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
        #if DEBUG
        return;
        #endif

        // ReSharper disable HeuristicUnreachableCode
        #pragma warning disable CS0162 // Unreachable code

        LogTitle("Clean up");
        try
        {
            if (Directory.Exists(SolutionDir))
            {
                Log($"Deleting temp dir: {SolutionDir}");
                Directory.Delete(SolutionDir, recursive: true);
            }
            else
            {
                Log($"Temp dir did not exist: {SolutionDir}");
            }
        }
        catch (Exception ex)
        {
            Log($"Could not delete temp dir: {ex.Message}");
        }

        Log(); // Extra for CI

        // ReSharper restore HeuristicUnreachableCode
        #pragma warning restore CS0162 // Unreachable code
    }

    void IDisposable.Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    ~TestBase() => Cleanup(); // ncrunch: no coverage
    
    // Logging

    /// <summary>
    /// Logs a line to the Debug or Console output.
    /// </summary>
    internal void Log(string message = "") => Trace.WriteLine(message);

    internal void LogTitle(string title = "")
    {
        Log();
        Log(title);
        string line = "-".Repeat(title.Length);
        Log(line);
    }

    // File Helpers

    internal bool   BuildNumXmlExists()              => Exists(BuildNumXmlFilePath);
    internal bool   DirPropsExists()                 => Exists(DirPropsFilePath);
    internal string ReadBuildNumXml()                => ReadAllText(BuildNumXmlFilePath);
    internal string ReadDirProps()                   => ReadAllText(DirPropsFilePath);
    internal void   WriteBuildNumXml(string content) => Save(BuildNumXmlFilePath, content);
    internal void   WriteDirProps(string content)    => Save(DirPropsFilePath, content);
    internal void   DeleteBuildNumXml()              => Delete(BuildNumXmlFilePath);
    internal void   DeleteDirProps()                 => Delete(DirPropsFilePath);

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
    
    // TODO: Move to JJ.Framework.
    internal static void ThrowIf(bool condition, [CallerArgumentExpression("condition")] string? argExpress = null)
    {
        if (condition) throw new Exception(argExpress);
    }

    /// <inheritdoc cref="_rebuildsincrement" />
    internal void RebuildsWith(int buildNum, int packNum)
    {
        int expectedBuildNum = buildNum;
        int actualBuildNum = GetBuildNumFromXml();
        IsTrue(actualBuildNum == expectedBuildNum);
        string output = Rebuild();
        string packageName = ExtractPackageFileName(output);
        IsTrue(packageName.EndsWith($".{packNum}.nupkg"));
    }
    
    /// <inheritdoc cref="_rebuildsincrement" />
    internal void RebuildsWith(int buildNum)
    {
        int packNum = buildNum;
        RebuildsWith(buildNum, packNum);
    }

    /// <inheritdoc cref="_rebuildsincrement" />
    internal void RebuildsIncrement(int repeats = DEFAULT_REPEATS)
    {
        int from = GetBuildNumFromXml(nolog);
        RebuildsIncrement(from, repeats);
    }

    /// <inheritdoc cref="_rebuildsincrement" />
    // ReSharper disable once UnusedParameter.Global
    // ReSharper disable once MethodOverloadWithOptionalParameter
    internal void RebuildsIncrement(int from, OverloadByName nameOvl = default) 
        => RebuildsIncrement(from, DEFAULT_REPEATS);

    /// <inheritdoc cref="_rebuildsincrement" />
    private void RebuildsIncrement(int from, int repeats)
    {
        ThrowIf(from < 0);
        ThrowIf(repeats > 10);
        int to = from + repeats - 1;
        for (int num = from; num <= to; num++)
        {
            bool isLast = num == to;
            RebuildsWith(num);
            if (!isLast) Log();
        }
    }
    
    internal void RebuildsFreezeVersion(int repeats = DEFAULT_REPEATS)
    {
        int frozen = GetBuildNumFromXml(nolog);
        for (int i = 0; i < repeats; i++)
        {
            bool isLast = i == repeats - 1;
            RebuildsWith(frozen);
            if (!isLast) Log();
        }    
    }

    /// <summary>
    /// The first build may fail because $(BuildNum) resolves to empty.
    /// This is expected per the manual plan.
    /// </summary>
    internal void RebuildExpectFail()
    {
        const string expected = "not a valid version string";

        string output;
        try
        {
            output = Rebuild();
        } // ncrunch: no coverage
        catch (Exception ex)
        {

            bool hasExpectedError =
                ex.Message.Contains(expected, OrdinalIgnoreCase) ||
                ex.Message.Contains("NETSDK1018", OrdinalIgnoreCase);

            // ncrunch: no coverage start
            if (!hasExpectedError)
            {
                throw new Exception($"First build failed but not with the expected error '{expected}'.", ex);
            }

            Log($"Build failed: '{expected}'");
            return;

        } // ncrunch: no coverage

        Log($"Build succeeded while expecting error: '{expected}'. Output: {output}"); // ncrunch: no coverage
    }

    internal string Rebuild()
    {
        Log("Rebuild");
        //return RunProcess("dotnet", $"build \"{CsprojFilePath}\" -c Release -v {Verbosity} --no-incremental --no-restore");
        return RunProcess("dotnet", $"msbuild \"{CsprojFilePath}\" /t:Rebuild /p:Configuration=Release /v:{VERBOSITY}");
    }

    internal string Rebuild(string? extraArgs)
    {
        Log($"Rebuild with {extraArgs}");
        //return RunProcess("dotnet"`, $"build \"{CsprojFilePath}\" -c Release -v {Verbosity} --no-incremental --no-restore {extraArgs}");
        return RunProcess("dotnet", $"msbuild \"{CsprojFilePath}\" /t:Rebuild /p:Configuration=Release /v:{VERBOSITY} {extraArgs}");
    }

    internal string RebuildDebug()
    {
        Log("Rebuild Debug");
        //return RunProcess("dotnet", $"build \"{CsprojFilePath}\" -c Debug -v {Verbosity} --no-incremental --no-restore");
        return RunProcess("dotnet", $"msbuild \"{CsprojFilePath}\" /t:Rebuild /p:Configuration=Debug /v:{VERBOSITY}");
    }

    internal void InstallPackage()
    {
        Log("Install package");
        string version = GetEmbeddedPackageVersion();
        RunProcess("dotnet", $"add \"{CsprojFilePath}\" package {PackageId} --version {version}");
        Restore();
      }

    internal void UninstallPackage()
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
            // TODO: Add Shim to JJ.Framework.
            #if !NET5_0_OR_GREATER
            process.Kill();
            #else
            process.Kill(entireProcessTree: true);
            #endif
            throw new TimeoutException($"{fileName} {arguments} timed out after {timeoutSeconds}s");
        }
        // ncrunch: no coverage end

        // .NET may flush async after WaitForExit(int); call the parameterless overload.
        process.WaitForExit();

        var output = outputSB.ToString().TrimEnd();
        var error = errorSB.ToString().Trim();

        if (string.Equals(VERBOSITY, "Diagnostic", OrdinalIgnoreCase) ||
            string.Equals(VERBOSITY, "Detailed", OrdinalIgnoreCase))
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

    internal int GetBuildNumFromXml()
    {
        int value = GetBuildNumFromXml(nolog);
        Log($"BuildNum.xml = {value}");
        return value;
    }

    // ReSharper disable once UnusedParameter.Global
    internal int GetBuildNumFromXml(NoLog nolog)
    {
        var doc = XDocument.Load(BuildNumXmlFilePath);
        var elements = doc.Descendants("BuildNum").ToArray();
        AreEqual(1, elements.Length);
        IsNotNull(elements[0]);
        string str = elements[0].Value;
        var value = int.Parse(str);
        return value;
    }

    internal void SetBuildNumInXml(int num)
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
    internal void SetProjPatchNum(string patch)
    {
        string majorMinor = GetCsprojMajorMinor();
        SetCsprojVersion($"{majorMinor}.{patch}"); // Logs
    }

    /// <summary>
    /// Checks whether the csproj currently references the package.
    /// </summary>
    internal bool CsprojHasPackageReference()
    {
        string text = ReadAllText(CsprojFilePath);
        var hasRef = text.Contains($"Include=\"{PackageId}\"", OrdinalIgnoreCase);
        string csprojFileName = Path.GetFileName(CsprojFilePath);

        if (hasRef)
        {
            Log($"{PackageId} ref exists in {csprojFileName}");
        }
        else
        {
            Log($"{PackageId} ref missing from {csprojFileName}");
        }
           
        return hasRef;
    }

    /// <summary>
    /// Removes the JJ.AutoIncrementVersion PackageReference from the csproj
    /// by editing the file directly (no dotnet CLI needed).
    /// </summary>
    internal void RemovePackageReferenceFromCsproj()
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
    internal string ExtractPackageFileName(string output)
    {
        Match match = Match(output, @"(JJ\.AutoIncrementVersion\.Test\.\S+\.nupkg)");
        string packageFileName = match.Success ? match.Groups[1].Value : "";

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
    internal void EnsureDirPropsReleaseCondition()
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
