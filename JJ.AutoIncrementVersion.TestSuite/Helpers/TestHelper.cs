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
    public string ProjectDir { get; }
    public string CsprojPath { get; }
    public string BuildNumXmlPath { get; }
    public string DirectoryBuildPropsPath { get; }

    private readonly TestContext _ctx;

    private const string PackageId = "JJ.AutoIncrementVersion";
    private const string PackageVersion = "4.2.5746";
    private const string TestProjectName = "JJ.AutoIncrementVersion.Test";

    // ── ctor ───────────────────────────────────────────────────────────
    public TestHelper(TestContext ctx)
    {
        _ctx = ctx;

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
        _ctx.WriteLine(message);
        Console.WriteLine(message);
    }

    public void LogStep(string step) => Log($"── STEP: {step}");
    public void LogResult(string result) => Log($"   ✓ {result}");
    public void LogWarning(string warning) => Log($"   ⚠ {warning}");

    // ── process execution ──────────────────────────────────────────────
    public record CommandResult(int ExitCode, string Output, string Error);

    public CommandResult RunDotnet(string arguments, int timeoutSeconds = 120)
    {
        Log($"   > dotnet {arguments}");

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

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(timeoutSeconds * 1000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"dotnet {arguments} timed out after {timeoutSeconds}s");
        }

        // .NET may flush async after WaitForExit(int); call the parameterless overload.
        process.WaitForExit();

        var result = new CommandResult(process.ExitCode, stdout.ToString(), stderr.ToString());

        if (result.Output.Length > 0) Log(result.Output.TrimEnd());
        if (result.Error.Length > 0) Log($"   [stderr] {result.Error.TrimEnd()}");
        return result;
    }

    /// <summary>Run dotnet against the solution directory (for solution-level commands).</summary>
    public CommandResult RunDotnetAtSolutionDir(string arguments, int timeoutSeconds = 120)
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

        var result = new CommandResult(process.ExitCode, stdout.ToString(), stderr.ToString());

        if (result.Output.Length > 0) Log(result.Output.TrimEnd());
        if (result.Error.Length > 0) Log($"   [stderr] {result.Error.TrimEnd()}");
        return result;
    }

    // ── build shortcuts ────────────────────────────────────────────────
    public CommandResult Build(string configuration = "Release", string? extraArgs = null)
    {
        string args = $"build \"{CsprojPath}\" -c {configuration}";
        if (extraArgs is not null) args += $" {extraArgs}";
        return RunDotnet(args);
    }

    public CommandResult Rebuild(string configuration = "Release")
        => Build(configuration, "--no-incremental");

    // ── package management ─────────────────────────────────────────────
    public CommandResult InstallPackage()
        => RunDotnet($"add \"{CsprojPath}\" package {PackageId} --version {PackageVersion}");

    public CommandResult UninstallPackage()
        => RunDotnet($"remove \"{CsprojPath}\" package {PackageId}");

    // ── file helpers ───────────────────────────────────────────────────
    public void DeleteBuildNumXml()
    {
        if (File.Exists(BuildNumXmlPath)) File.Delete(BuildNumXmlPath);
        Log($"   Deleted BuildNum.xml (exists={File.Exists(BuildNumXmlPath)})");
    }

    public void DeleteDirectoryBuildProps()
    {
        if (File.Exists(DirectoryBuildPropsPath)) File.Delete(DirectoryBuildPropsPath);
        Log($"   Deleted Directory.Build.props (exists={File.Exists(DirectoryBuildPropsPath)})");
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
        GitRestoreAll();
        UninstallPackage();
        DeleteBuildNumXml();
        DeleteDirectoryBuildProps();
        SetCsprojVersion("4.3.0");
        LogResult("Initial state set (package removed, xml/props deleted, version=4.3.0)");
    }

    /// <summary>
    /// Restore repo back to its committed state after each test.
    /// </summary>
    public void Cleanup()
    {
        Log("── CLEANUP ──");
        GitRestoreAll();
        // Also restore potentially deleted untracked files? BuildNum.xml is tracked,
        // Directory.Build.props is tracked, so git checkout restores them.
        LogResult("Repo restored to committed state");
    }
}
