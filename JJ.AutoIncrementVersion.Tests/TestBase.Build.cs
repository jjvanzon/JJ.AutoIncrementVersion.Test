using JJ.Framework.Compilation.Core;

namespace JJ.AutoIncrementVersion.Tests;

public partial class TestBase
{
    private DotNetOptions Options { get; set; }

    private void InitDotNetOptions()
    {
        Options = new() { Dir = ProjectDir, File = CsprojFilePath, TimeOutSec = 120 };
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

        } // ncrunch: no coverage

        Log($"Build succeeded while expecting error: '{expected}'. Output: {output}"); // ncrunch: no coverage
    }

    internal string Rebuild()
    {
        Log("Rebuild");
        //return DotNetBuild($"-c Release -v {Verbosity} --no-incremental --no-restore");
        return MSBuild($"/t:Rebuild /p:Configuration=Release /v:{VERBOSITY}");
    }

    internal string Rebuild(string extraArgs)
    {
        Log($"Rebuild with {extraArgs}");
        //return DotNetBuild($"-c Release -v {Verbosity} --no-incremental --no-restore {extraArgs}");
        return MSBuild($"/t:Rebuild /p:Configuration=Release /v:{VERBOSITY} {extraArgs}");
    }

    internal string RebuildDebug()
    {
        Log("Rebuild Debug");
        //return DotNetBuild($"-c Debug -v {Verbosity} --no-incremental --no-restore");
        return MSBuild($"/t:Rebuild /p:Configuration=Debug /v:{VERBOSITY}");
    }

    internal void InstallPackage()
    {
        Log("Install package");
        string version = GetEmbeddedPackageVersion();
        DotNetInstallPackage($"package {PackageId} --version {version}");
        Restore();
      }

    internal void UninstallPackage()
    {
        Log("Uninstall package");
        DotNetUninstallPackage($"package {PackageId}");
        Restore(); // Or uninstall isn't finalized somehow.
    }
    
    private void Restore()
    {
        Log("Restore");
        DotNetRestore();
    }

    private string MSBuild(string args)
        => LogIfNeeded(DotNet.MSBuild(Options with { Args = args }));

    private string DotNetBuild(string args)
        => LogIfNeeded(DotNet.Build(Options with { Args = args }));

    private string DotNetExe(string args)
        => LogIfNeeded(DotNet.Exe(Options with { Args = args }));

    private string DotNetRestore()
        => LogIfNeeded(DotNet.Exe(Options with { Command = "restore" }));

    private string DotNetInstallPackage(string args)
        => LogIfNeeded(DotNet.Exe(Options with { Command = "add", Args = args }));

    private string DotNetUninstallPackage(string args)
        => LogIfNeeded(DotNet.Exe(Options with { Command = "remove", Args = args }));


    private static string LogIfNeeded(string output)
    {
        if (string.Equals(VERBOSITY, "Diagnostic", OrdinalIgnoreCase) ||
            string.Equals(VERBOSITY, "Detailed", OrdinalIgnoreCase))
        // ncrunch: no coverage start
        {
            Log(output);
        }
        // ncrunch: no coverage end

        return output;
    }
}
