
// TODO: Build and Rebuild error out, with error "README.md does not exist", probably because the csproj references $(SolutionDir) does not exist, because wse build a csproj, not the solution. Keep buildingcsproj. $(SolutionDir) reference should change.

// TODO: Maybe rename specifically to "CommandLineResult", because "Command" makes me think of more things than command line.


    // TODO: Only used for logging. Might Debug and Console be enough? Saves us a dependency/init/cleanup/property.
    private readonly TestContext _ctx;

        _ctx = ctx;


    // ── ctor ───────────────────────────────────────────────────────────
    public TestHelper()
    {

        // TODO: Do once (static)
        // TODO: sln file isn't even there in an NCrunch context.

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

        private const string TestProjectName = "JJ.AutoIncrementVersion.Test";



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

        process.OutputDataReceived += (_, e) => { stdout.AppendLine(e.Data ?? ""); };
        process.ErrorDataReceived += (_, e) => { stderr.AppendLine(e.Data ?? ""); };
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

// = "..\\JJ.AutoIncrementVersion.Test";
//= ProjectDir + "\\JJ.AutoIncrementVersion.Test.csproj";