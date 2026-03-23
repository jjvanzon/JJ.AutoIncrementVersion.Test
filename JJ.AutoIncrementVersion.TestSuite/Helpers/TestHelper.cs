using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace JJ.AutoIncrementVersion.TestSuite.Helpers;

/// <summary>
/// Helpers for running dotnet CLI commands, manipulating project files,
/// and inspecting build output — used by the automated test-plan tests.
/// </summary>
public sealed class TestHelper
{
    // ── paths ──────────────────────────────────────────────────────────
    public string SolutionDir { get; }
    public string ProjectDir { get; } // = "..\\JJ.AutoIncrementVersion.Test";
    public string CsprojPath { get; } //= ProjectDir + "\\JJ.AutoIncrementVersion.Test.csproj";
    public string BuildNumXmlPath { get; }
    public string DirectoryBuildPropsPath { get; }

    private const string PackageId = "JJ.AutoIncrementVersion";
    // TODO: Fixed version number not good. Latest from Pre-Release-Package-Feed is better. Kinda bad, because if we keep this, we'd assume the tests test the latest, which it never would.
    private const string PackageVersion = "4.2.5746";
    private const string TestProjectName = "JJ.AutoIncrementVersion.Test";

    // ── ctor ───────────────────────────────────────────────────────────
    public TestHelper()
    {

        // TODO: Do once (static)
        // TODO: sln file isn't even there in an NCrunch context.
        // TODO: Can we make assumptions about (relative) locations?
        // TODO: Maybe we should copy the necessary test project files to somewhere reachable by a test. Maybe as embedded resources in the .TestSuite project, that link to the files, so that they are compiled into the .TestSuite project and then saved to disk relative to the .TestSuite.dll so all files everything resolve. That might even help us with test isolation/parallel tests.

        // Walk up from the test assembly's location to find the repo root.
        // The repo root contains Directory.Build.props / BuildNum.xml.
        string assemblyDir = Path.GetDirectoryName(typeof(TestHelper).Assembly.Location)!;
        string? dir = assemblyDir;
        while (dir is not null && !File.Exists(Path.Combine(dir, "JJ.AutoIncrementVersion.Test.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        SolutionDir = dir ?? throw new InvalidOperationException(
            $"Could not find solution root walking up from {assemblyDir}");

        ProjectDir = Path.Combine(SolutionDir, TestProjectName);
        CsprojPath = Path.Combine(ProjectDir, $"{TestProjectName}.csproj");
        BuildNumXmlPath = Path.Combine(SolutionDir, "BuildNum.xml");
        DirectoryBuildPropsPath = Path.Combine(SolutionDir, "Directory.Build.props");
    }

    // ── logging ────────────────────────────────────────────────────────
    public void Log(string message)
    {
        #if DEBUG
        Debug.WriteLine(message);
        #else
        Console.WriteLine(message);
        #endif
    }

    public void LogStep(string step) => Log($"── STEP: {step}");
    public void LogResult(string result) => Log($"   ✓ {result}");
    public void LogWarning(string warning) => Log($"   ⚠ {warning}");

    // ── process execution ──────────────────────────────────────────────
    public record CommandLineResult(int ExitCode, string Output, string Error);

    public CommandLineResult RunDotnet(string arguments, int timeoutSeconds = 120)
    {
        Log($"   > dotnet {arguments}");

        // TODO: Lots of ceremony could be reused for multiple ProcessStart helpers.

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = ProjectDir,
            RedirectStandardOutput = true, // REVIEWED: Interesting. Didn't know.
            RedirectStandardError = true, // REVIEWED: Interesting. Didn't know.
            UseShellExecute = false,
            CreateNoWindow = true // REVIEWED: Interesting. Didn't know.
        };

        using var process = Process.Start(psi)!;
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        // REVIEWED: Cool. Didn't know before how to capture output. Bit verbose from .NET, but nice that it's possible
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); }; // TODO: e.Data ?? "" would prevent null pattern check.
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); }; // TODO: e.Data ?? "" would prevent null pattern check.
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(timeoutSeconds * 1000)) // TODO: Infra-specific 2 min time-out should be central variable and even from config.
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"dotnet {arguments} timed out after {timeoutSeconds}s");
        }

        // .NET may flush async after WaitForExit(int); call the parameterless overload. // REVIEWED: Cool. Didn't know.
        process.WaitForExit();

        var result = new CommandLineResult(process.ExitCode, stdout.ToString(), stderr.ToString());
        // TODO: Should check exit code here already? Fail fast?
        if (result.Output.Length > 0) Log(result.Output.TrimEnd());
        if (result.Error.Length > 0) Log($"   [stderr] {result.Error.TrimEnd()}"); // TODO: Throw instead to stop test?
        return result;
    }

    /// <summary>Run dotnet against the solution directory (for solution-level commands).</summary>
    public CommandLineResult RunDotnetAtSolutionDir(string arguments, int timeoutSeconds = 120)
    {
        Log($"   > dotnet {arguments}  (solution dir)");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = SolutionDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(timeoutSeconds * 1000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"dotnet {arguments} timed out after {timeoutSeconds}s");
        }

        process.WaitForExit();

        var result = new CommandLineResult(process.ExitCode, stdout.ToString(), stderr.ToString());

        if (result.Output.Length > 0) Log(result.Output.TrimEnd());
        if (result.Error.Length > 0) Log($"   [stderr] {result.Error.TrimEnd()}");
        return result;
    }

    // ── build shortcuts ────────────────────────────────────────────────
    public CommandLineResult Build(string configuration = "Release", string? extraArgs = null)
    {
        string args = $"build \"{CsprojPath}\" -c {configuration}";
        if (extraArgs is not null) args += $" {extraArgs}";
        return RunDotnet(args);
    }

    public CommandLineResult Rebuild(string configuration = "Release")
        => Build(configuration, "--no-incremental");

    // ── package management ─────────────────────────────────────────────
    public CommandLineResult InstallPackage()
        => RunDotnet($"add \"{CsprojPath}\" package {PackageId} --version {PackageVersion}");

    // TODO: Command errors out hard. Note sure if it's correct:
    // > dotnet remove "D:\Repositories\JJ.AutoIncrementVersion.Test\JJ.AutoIncrementVersion.Test\JJ.AutoIncrementVersion.Test.csproj" package JJ.AutoIncrementVersion
   // [stderr] Found more than one project in `D:\Repositories\JJ.AutoIncrementVersion.Test\JJ.AutoIncrementVersion.Test`. Specify which one to use.

    public CommandLineResult UninstallPackage()
        => RunDotnet($"remove \"{CsprojPath}\" package {PackageId}");

    // ── file helpers ───────────────────────────────────────────────────
    public void DeleteBuildNumXml()
    {
        if (File.Exists(BuildNumXmlPath)) File.Delete(BuildNumXmlPath);
        Log($"   Deleted BuildNum.xml (exists={File.Exists(BuildNumXmlPath)})"); // TODO: Logs it's deleted even when it didn't evne exist.
    }

    public void DeleteDirectoryBuildProps()
    {
        if (File.Exists(DirectoryBuildPropsPath)) File.Delete(DirectoryBuildPropsPath);
        Log($"   Deleted Directory.Build.props (exists={File.Exists(DirectoryBuildPropsPath)})"); // TODO: Logs it's deleted even when it didn't evne exist.
    }

    public bool BuildNumXmlExists() => File.Exists(BuildNumXmlPath);
    public bool DirectoryBuildPropsExists() => File.Exists(DirectoryBuildPropsPath);

    public string ReadBuildNumXml() => File.ReadAllText(BuildNumXmlPath);
    public string ReadDirectoryBuildProps() => File.ReadAllText(DirectoryBuildPropsPath);

    public void WriteBuildNumXml(string content) => File.WriteAllText(BuildNumXmlPath, content);
    public void WriteDirectoryBuildProps(string content) => File.WriteAllText(DirectoryBuildPropsPath, content);

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
        File.WriteAllText(BuildNumXmlPath, doc.Declaration?.ToString() ?? "");
        using var writer = new System.Xml.XmlTextWriter(BuildNumXmlPath, Encoding.UTF8)
        {
            Formatting = System.Xml.Formatting.None
        };
        doc.WriteTo(writer);
    }

    /// <summary>
    /// Replaces the <c>&lt;Version&gt;</c> value in the csproj.
    /// </summary>
    public void SetCsprojVersion(string version)
    {
        string text = File.ReadAllText(CsprojPath);
        text = Regex.Replace(text, @"<Version>[^<]*</Version>", $"<Version>{version}</Version>");
        File.WriteAllText(CsprojPath, text);
        Log($"   Set <Version> to {version}");
    }

    /// <summary>
    /// Checks whether the csproj currently references the package.
    /// </summary>
    public bool CsprojHasPackageReference()
    {
        string text = File.ReadAllText(CsprojPath);
        return text.Contains($"Include=\"{PackageId}\"", StringComparison.OrdinalIgnoreCase);
    }

    // ── output inspection ──────────────────────────────────────────────

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

    // ── git restore ────────────────────────────────────────────────────
    /// <summary>
    /// Restores all modified/tracked files in the repo via git checkout, 
    /// so the next test starts clean.
    /// </summary>
    public void GitRestoreAll()
    {
        Log("   SKIPPED GIT RESET. THE TEST SHOULD NOT ERASE OUR EDITS!!!");
        return; // REVIEWED: Added `return`: Not a good plan to do this. It may wipe out changes to our test code.
        Log("   Restoring repo to clean state via git...");
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "checkout -- .",
            WorkingDirectory = SolutionDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi)!;
        p.WaitForExit(30_000);
        LogResult("Repo restored to committed state"); // CHANGED: Moved from CleanUp method.

    }

    /// <summary>
    /// Full "Set Initial State" as described in the manual test plan:
    /// uninstall package, delete BuildNum.xml &amp; Directory.Build.props,
    /// replace $(BuildNum) with 0 in csproj.
    /// </summary>
    public void SetInitialState()
    {
        LogStep("Set Initial State");
        // Restore tracked files first so csproj is in its committed state.
        //GitRestoreAll(); // REVIEWED: Commented out. Not a good plan to do this. It may wipe out changes to our test code.

        UninstallPackage(); // TODO: Output result is ignored. If fails it. It would just blindly continue the next steps.
        DeleteBuildNumXml();
        DeleteDirectoryBuildProps();
        SetCsprojVersion("4.3.0"); // TODO: Should not assume the version will start with 4.3. Should use current value for that major/minor.
        LogResult("Initial state set (package removed, xml/props deleted, version=4.3.0)"); // REVIEWED: Nice. Clear information in log. // TODO: False though, because errors are ignored. So package was actually not removed.
    }

    // TODO: Cleanup does nothing as GitRestoreAll is omitted. Remove clean-up logic completely? 

    /// <summary>
    /// Restore repo back to its committed state after each test.
    /// </summary>
    public void Cleanup()
    {
        Log("── CLEANUP ──");
        //GitRestoreAll();  // REVIEWED: Commented out. Not a good plan to do this. It may wipe out changes to our test code.
        // Also restore potentially deleted untracked files? BuildNum.xml is tracked,
        // Directory.Build.props is tracked, so git checkout restores them.
        //LogResult("Repo restored to committed state"); // CHANGED: Moved to GitRestoreAll method.
    }
}
