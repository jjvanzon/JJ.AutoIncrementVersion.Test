
namespace JJ.AutoIncrementVersion.Tests;

public partial class TestBase
{
    private DotNetOptions Options { get; set; }

    private void InitDotNetOptions()
    {
        Options = new() 
        { 
            Dir = ProjectDir, 
            File = CsprojFilePath, 
            BuildConf = "Release",
            TimeOutSec = 120,
            Verbosity = Detailed,
            Log = Log
         };
    }

    // Run Processes

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

        }

        Log($"Build succeeded while expecting error: '{expected}'. Output: {output}"); 
        // ncrunch: no coverage end
    }

    internal string Rebuild()
    {
        Log("Rebuild");
        //return DotNet.Build($"--no-incremental", Options);
        return MSRebuild();
    }

    internal string Rebuild(string extraArgs)
    {
        Log($"Rebuild with {extraArgs}");
        //return DotNet.Build($"--no-incremental {extraArgs}", Options);
        return MSRebuild(extraArgs);
    }

    internal string RebuildDebug()
    {
        Log("Rebuild Debug");
        //return DotNet.Build($"-c Debug --no-incremental", Options);
        return MSRebuild($"/p:Configuration=Debug");
    }

    internal void InstallPackage()
    {
        Log("Install package");
        string version = GetEmbeddedPackageVersion();
        DotNet.InstallPackage(PackageId, version, Options);
        Restore();
    }

    internal void UninstallPackage()
    {
        Log("Uninstall package");
        DotNet.UninstallPackage(PackageId, Options);
        Restore(); // Or uninstall isn't finalized somehow.
    }
    
    private void Restore()
    {
        Log("Restore");
        DotNet.Restore(Options);
    }

    private string MSRebuild(string args = "")
    {
        // This didn't work, because in many cases no explicit restore is done, and a restore of all TargetFrameworks seems necessary.
        //args += $" -p:TargetFramework={DotNet.RunningTargetFramework}";
        return DotNet.MSRebuild(args, Options);
    }
}
