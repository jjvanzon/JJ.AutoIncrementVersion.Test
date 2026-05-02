namespace JJ.AutoIncrementVersion.Tests;

public partial class TestBase
{
    private const int CmdTimeOutSeconds = 120;
        
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

        if (!process.WaitForExit(CmdTimeOutSeconds * 1000))
        // ncrunch: no coverage start
        {
            // TODO: Add Shim to JJ.Framework.
            #if !NET5_0_OR_GREATER
            process.Kill();
            #else
            process.Kill(entireProcessTree: true);
            #endif
            throw new TimeoutException($"{fileName} {arguments} timed out after {CmdTimeOutSeconds}s");
        }
        // ncrunch: no coverage end

        // .NET may flush async after WaitForExit(int); call the parameterless overload.
        process.WaitForExit();

        var output = outputSB.ToString().TrimEnd();
        var error = errorSB.ToString().Trim();

        if (string.Equals(VERBOSITY, "Diagnostic", OrdinalIgnoreCase) ||
            string.Equals(VERBOSITY, "Detailed", OrdinalIgnoreCase))
        // ncrunch: no coverage start
        {
            Log($"Exit Code = {process.ExitCode}");
            Log($"Error = {error}");
            Log($"Output = {output}");
        }
        // ncrunch: no coverage end

        bool hasExitCode = process.ExitCode != 0;
        bool hasErrorText = !IsNullOrWhiteSpace(error);
        bool hasErrorInOutput = output.Contains("[error]");
        bool hasError = hasExitCode || hasErrorInOutput; // Don't consider error text, which has welcome messages and such in it these days.

        if (hasError)
        {
            throw new Exception(
                $"{fileName} {arguments} failed " +
                $"{new { hasExitCode, hasErrorText, hasErrorInOutput }}: " +
                $"Exit code {process.ExitCode} {error} {output}");
        }

        return $"{error} {output}";
    }
}
