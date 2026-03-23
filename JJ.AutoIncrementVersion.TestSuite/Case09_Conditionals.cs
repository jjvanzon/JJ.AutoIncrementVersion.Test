using static System.StringComparison;

namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case09_Conditionals
{
    /// <summary>
    /// Manual Test Plan → "Conditionals"
    ///
    /// Tests conditional BuildNum.xml inclusion from Directory.Build.props.
    ///
    /// The committed Directory.Build.props already has the condition:
    ///   Condition="Exists('BuildNum.xml') And $(Configuration)=='Release'"
    ///
    /// Steps:
    ///   1. Verify Directory.Build.props has the Release condition.
    ///   2. Build for Release – BuildNum increments.
    ///   3. Build for Debug – uses BuildNum 0.
    ///   4. Swap a few times to see if Release continues with original range.
    /// </summary>
    [TestMethod]
    public void Case09_Conditionals_ReleaseIncrementsDebugUsesZero()
    {
        using var testHelper = new TestHelper();

        testHelper.SetInstalledState();

        string dirPropsContent = testHelper.ReadDirectoryBuildProps();

        // ── Verify Directory.Build.props has the conditional ──
        // TODO: Assert can fail because the Release condition is not guaranteed as an initial state.
        IsTrue(dirPropsContent.Contains("$(Configuration)=='Release'", OrdinalIgnoreCase));

        // ── Release build – should increment ──
        var releaseBuildResult1 = testHelper.Build("Release");
        AreEqual(0, releaseBuildResult1.ExitCode, $"Release build 1 failed.\n{releaseBuildResult1.Error}");
        int? releaseBuildNum1 = testHelper.ExtractBuildNumFromNupkgName(releaseBuildResult1.Output);
        testHelper.LogResult($"Release build 1: nupkg num={releaseBuildNum1}");

        testHelper.LogStep("Release build 2");
        CommandLineResult releaseBuild2Result = testHelper.Build("Release");
        AreEqual(0, releaseBuild2Result.ExitCode, $"Release build 2 failed.\n{releaseBuild2Result.Error}");
        int? releaseBuildNum2 = testHelper.ExtractBuildNumFromNupkgName(releaseBuild2Result.Output);
        testHelper.LogResult($"Release build 2: nupkg num={releaseBuildNum2}");

        if (releaseBuildNum1 is not null && releaseBuildNum2 is not null)
        {
            IsTrue(releaseBuildNum2 > releaseBuildNum1);
        }

        // ── Debug build – should use BuildNum 0 ──
        testHelper.LogStep("Debug build – should use BuildNum 0");
        var dbg = testHelper.Build("Debug");
        AreEqual(0, dbg.ExitCode, $"Debug build failed.\n{dbg.Error}");

        int? dbgNum = testHelper.ExtractBuildNumFromNupkgName(dbg.Output);
        testHelper.LogResult($"Debug build: nupkg num={dbgNum}");

        // Debug build may or may not produce a nupkg depending on GeneratePackageOnBuild,
        // but if it does, the BuildNum portion should be 0.
        if (dbgNum is not null)
        {
            AreEqual(0, dbgNum.Value, $"Debug build should use BuildNum 0 but got {dbgNum}");
        }
        else
        {
            testHelper.LogWarning("Debug build did not produce a nupkg name in output – checking not possible.");
        }

        // ── Swap back to Release – should continue incrementing ──
        testHelper.LogStep("Back to Release – should continue from where it left off");
        var rel3 = testHelper.Build("Release");
        AreEqual(0, rel3.ExitCode, $"Release build 3 failed.\n{rel3.Error}");
        int? relNum3 = testHelper.ExtractBuildNumFromNupkgName(rel3.Output);
        testHelper.LogResult($"Release build 3: nupkg num={relNum3}");

        if (releaseBuildNum2 is not null && relNum3 is not null)
        {
            IsTrue(relNum3 > releaseBuildNum2,
                $"Release should continue incrementing: rel2={releaseBuildNum2}, rel3={relNum3}");
        }

        // ── One more Debug to confirm ──
        testHelper.LogStep("Debug again – still BuildNum 0");
        var dbg2 = testHelper.Build("Debug");
        AreEqual(0, dbg2.ExitCode, $"Debug build 2 failed.\n{dbg2.Error}");
        int? dbgNum2 = testHelper.ExtractBuildNumFromNupkgName(dbg2.Output);
        testHelper.LogResult($"Debug build 2: nupkg num={dbgNum2}");

        if (dbgNum2 is not null)
        {
            AreEqual(0, dbgNum2.Value, $"Debug build should still use BuildNum 0 but got {dbgNum2}");
        }

        // ── One more Release ──
        testHelper.LogStep("Release again – still incrementing");
        var rel4 = testHelper.Build("Release");
        AreEqual(0, rel4.ExitCode);
        int? relNum4 = testHelper.ExtractBuildNumFromNupkgName(rel4.Output);
        testHelper.LogResult($"Release build 4: nupkg num={relNum4}");

        if (relNum3 is not null && relNum4 is not null)
        {
            IsTrue(relNum4 > relNum3,
                $"Release should keep incrementing: rel3={relNum3}, rel4={relNum4}");
        }

        testHelper.LogResult("PASS – Conditionals: Release increments, Debug uses 0, swapping works");
    }
}
