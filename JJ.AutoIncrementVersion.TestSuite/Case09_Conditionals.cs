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
        string releaseBuildOutput1 = testHelper.Rebuild();
        int? releaseBuildNum1 = testHelper.ExtractBuildNumFromNupkgName(releaseBuildOutput1);
        testHelper.LogResult($"Release build 1: nupkg num={releaseBuildNum1}");

        string releaseBuildOutput2 = testHelper.Rebuild();
        int? releaseBuildNum2 = testHelper.ExtractBuildNumFromNupkgName(releaseBuildOutput2);
        testHelper.LogResult($"Release build 2: nupkg num={releaseBuildNum2}");

        // TODO: Assert exact increment by 1
        if (releaseBuildNum1 is not null && releaseBuildNum2 is not null)
        {
            IsTrue(releaseBuildNum2 > releaseBuildNum1);
        }

        // ── Debug build – should use BuildNum 0 ──
        testHelper.LogStep("Debug build – should use BuildNum 0");
        string debugBuildOutput = testHelper.RebuildDebug();
        int? debugBuildNum = testHelper.ExtractBuildNumFromNupkgName(debugBuildOutput);
        testHelper.LogResult($"Debug build: nupkg num={debugBuildNum}");

        // Debug build may or may not produce a nupkg depending on GeneratePackageOnBuild,
        // but if it does, the BuildNum portion should be 0.
        if (debugBuildNum is not null)
        {
            AreEqual(0, debugBuildNum.Value, $"Debug build should use BuildNum 0 but got {debugBuildNum}");
        }
        else
        {
            testHelper.LogWarning("Debug build did not produce a nupkg name in output – checking not possible.");
        }

        // ── Swap back to Release – should continue incrementing ──
        testHelper.LogStep("Back to Release – should continue from where it left off");
        var rel3 = testHelper.Rebuild();
        int? relNum3 = testHelper.ExtractBuildNumFromNupkgName(rel3);
        testHelper.LogResult($"Release build 3: nupkg num={relNum3}");

        if (releaseBuildNum2 is not null && relNum3 is not null)
        {
            IsTrue(relNum3 > releaseBuildNum2,
                $"Release should continue incrementing: rel2={releaseBuildNum2}, rel3={relNum3}");
        }

        // ── One more Debug to confirm ──
        testHelper.LogStep("Debug again – still BuildNum 0");
        var dbg2 = testHelper.RebuildDebug();
        int? dbgNum2 = testHelper.ExtractBuildNumFromNupkgName(dbg2);
        testHelper.LogResult($"Debug build 2: nupkg num={dbgNum2}");

        if (dbgNum2 is not null)
        {
            AreEqual(0, dbgNum2.Value, $"Debug build should still use BuildNum 0 but got {dbgNum2}");
        }

        // ── One more Release ──
        testHelper.LogStep("Release again – still incrementing");
        var rel4 = testHelper.Rebuild();
        int? relNum4 = testHelper.ExtractBuildNumFromNupkgName(rel4);
        testHelper.LogResult($"Release build 4: nupkg num={relNum4}");

        if (relNum3 is not null && relNum4 is not null)
        {
            IsTrue(relNum4 > relNum3,
                $"Release should keep incrementing: rel3={relNum3}, rel4={relNum4}");
        }
    }
}
