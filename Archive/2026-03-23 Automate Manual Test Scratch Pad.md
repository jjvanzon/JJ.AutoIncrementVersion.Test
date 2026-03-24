
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

        // TODO: Lots of ceremony could be reused for multiple ProcessStart helpers.

// TODO: Infra-specific 2 min time-out should be central variable and even from config.

// TODO: Sub folder `Tests` not necessary. Move test classes up a directory.
// TODO: None of the tests can run in parallel or must run in isolation.



    public void DeleteBuildNumXml()
    {
        bool exists = Exists(BuildNumXmlPath);
        if (exists) 
        {
            Delete(BuildNumXmlPath);
            Log($"   Deleted BuildNum.xml ({BuildNumXmlPath}"); 
        }
        else
        {
            Log($"   Delete BuildNum.xml: Already Missing ({BuildNumXmlPath})");
        }
    }

    public void DeleteDirectoryBuildProps()
    {
        if (Exists(DirectoryBuildPropsPath)) Delete(DirectoryBuildPropsPath);
        Log($"   Deleted Directory.Build.props (exists={Exists(DirectoryBuildPropsPath)})"); // TODO: Logs it's deleted even when it didn't evne exist.
    }

    /// <summary>Root of the isolated temp folder (contains BuildNum.xml, Directory.Build.props).</summary>
    /// <summary>Sub-folder containing the csproj (one level deeper).</summary>

            // TODO: The error does not contain "Invalid NuGet version string" The actual error is: C:\Program Files\dotnet\sdk\10.0.201\NuGet.targets(196,5): error : '4.3.' is not a valid version string. (Parameter 'value') [D:\Repositories\JJ.AutoIncrementVersion.Test\JJ.AutoIncrementVersion.Test\JJ.AutoIncrementVersion.Test.csproj]


        // TODO: Use AssertCore (global using static) from JJ.Framework.Testing.Core (from JJs-Dev-Package-Feed)

            // TODO: ExitCode can indicate error, withotu Error text being filled in (error shows up in r.Output instead).
            AreEqual(0, result.ExitCode, $"Build {i + 1} failed.\n{result.Error}");

            //string errorText = result.Error;
            //if (string.IsNullOrWhiteSpace(errorText))
            //{
            //    errorText = result.Output;
            //}



    private void ExtractAllResources()
    {
        ExtractResourceBuildNumXml();
        ExtractResourceDirectoryBuildProps();
        ExtractResourceCsproj();
        ExtractResourceDummyTxt();
        ExtractResourceReadMe();
        ExtractResourceNuGetConfig();
    }

        //Log($"> {fileName} {arguments}");

        //if (result.Output.Length > 0) Log(result.Output.TrimEnd());
        //if (result.Error.Length > 0) Log($"[stderr] {result.Error.TrimEnd()}");


    // Embedded resource logical names
    //private const string CsprojFileName = "JJ.AutoIncrementVersion.Test.csproj";
    //private const string DirBuildPropsFileName = "Directory.Build.props";
    //private const string BuildNumXmlFileName = "BuildNum.xml";
    //private const string ReadMeFileName = "README.md";
    //private const string DummyTxtFileName = "Dummy.txt";
    //private const string NuGetConfigFileName = "NuGet.config";


        return;

        int buildNumFromXml = testHelper.GetBuildNumFromXml();
        AreEqual(1, buildNumFromXml);

        int buildNumFromOutput = testHelper.ExtractPackageBuildNum(buildOutput0);

        for (int i = 0; i < 3; i++)
        {
            var nextBuildOutput = testHelper.Rebuild();
            int nextBuildNum = testHelper.ExtractPackageBuildNum(nextBuildOutput);
            IsTrue(nextBuildNum > buildNumFromOutput);
            buildNumFromOutput = nextBuildNum;
        }

Literal repeat Rebuilds:

```cs

    // From Case07_AutoRecreateFiles:

        int buildNum1;
        {
            int buildNum = GetBuildNumFromXml();
            string output = Rebuild();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith($".{buildNum}.nupkg"));
            Log();
            buildNum1 = buildNum;
        }
        int buildNum2;
        {
            int buildNum = GetBuildNumFromXml();
            string output = Rebuild();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith($".{buildNum}.nupkg"));
            Log();
            buildNum2 = buildNum;
        }
        IsTrue(buildNum2 == buildNum1 + 1);

        int buildNum3;
        {
            int buildNum = GetBuildNumFromXml();
            string output = Rebuild();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith($".{buildNum}.nupkg"));
            Log();

            buildNum3 = buildNum;
        }
        IsTrue(buildNum3 == buildNum2 + 1);


    // From Case06_Reinstall:


        Log("OLD");
        int buildNum1 = GetBuildNumFromXml();
        string output1 = Rebuild();
        string packageName1 = ExtractPackageFileName(output1);
        IsTrue(packageName1.EndsWith($".{buildNum1}.nupkg"));
        Log();

        int buildNum2 = GetBuildNumFromXml();
        string output2 = Rebuild();
        string packageName2 = ExtractPackageFileName(output2);
        IsTrue(packageName2.EndsWith($".{buildNum2}.nupkg"));
        AreEqual(buildNum1 + 1, buildNum2);
        Log();

        int buildNum3 = GetBuildNumFromXml();
        string output3 = Rebuild();
        string packageName3 = ExtractPackageFileName(output3);
        IsTrue(packageName3.EndsWith($".{buildNum3}.nupkg"));
        AreEqual(buildNum2 + 1, buildNum3);
        Log();
```