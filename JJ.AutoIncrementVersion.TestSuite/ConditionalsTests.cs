namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class ConditionalsTests
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
    public void Conditionals_ReleaseIncrementsDebugUsesZero()
    {
        using var testHelper = new TestHelper();

        // ── Restore committed state (which already has the conditional) ──
        testHelper.LogStep("Restore committed state");
        testHelper.RestoreAll();

        // ── Verify Directory.Build.props has the conditional ──
        testHelper.LogStep("Verify Directory.Build.props has Release condition");
        string propsContent = testHelper.ReadDirectoryBuildProps();
        testHelper.LogResult($"Directory.Build.props: {propsContent.Trim()}");
        // Assert can fail because the Release condition is not guaranteed as an initial state.
        Assert.IsTrue(
            propsContent.Contains("$(Configuration)=='Release'", StringComparison.OrdinalIgnoreCase),
            "Directory.Build.props should have the Release condition.");

        // ── Release build – should increment ──
        testHelper.LogStep("Release build 1");
        var rel1 = testHelper.Build("Release");
        Assert.AreEqual(0, rel1.ExitCode, $"Release build 1 failed.\n{rel1.Error}");
        int? relNum1 = testHelper.ExtractBuildNumFromNupkg(rel1.Output);
        testHelper.LogResult($"Release build 1: nupkg num={relNum1}");

        testHelper.LogStep("Release build 2");
        var rel2 = testHelper.Build("Release");
        Assert.AreEqual(0, rel2.ExitCode, $"Release build 2 failed.\n{rel2.Error}");
        int? relNum2 = testHelper.ExtractBuildNumFromNupkg(rel2.Output);
        testHelper.LogResult($"Release build 2: nupkg num={relNum2}");

        if (relNum1 is not null && relNum2 is not null)
        {
            Assert.IsTrue(relNum2 > relNum1,
                $"Release should increment: rel1={relNum1}, rel2={relNum2}");
        }

        // ── Debug build – should use BuildNum 0 ──
        testHelper.LogStep("Debug build – should use BuildNum 0");
        var dbg = testHelper.Build("Debug");
        Assert.AreEqual(0, dbg.ExitCode, $"Debug build failed.\n{dbg.Error}");

        int? dbgNum = testHelper.ExtractBuildNumFromNupkg(dbg.Output);
        testHelper.LogResult($"Debug build: nupkg num={dbgNum}");

        // Debug build may or may not produce a nupkg depending on GeneratePackageOnBuild,
        // but if it does, the BuildNum portion should be 0.
        if (dbgNum is not null)
        {
            Assert.AreEqual(0, dbgNum.Value,
                $"Debug build should use BuildNum 0 but got {dbgNum}");
        }
        else
        {
            testHelper.LogWarning("Debug build did not produce a nupkg name in output – checking not possible.");
        }

        // ── Swap back to Release – should continue incrementing ──
        testHelper.LogStep("Back to Release – should continue from where it left off");
        var rel3 = testHelper.Build("Release");
        Assert.AreEqual(0, rel3.ExitCode, $"Release build 3 failed.\n{rel3.Error}");
        int? relNum3 = testHelper.ExtractBuildNumFromNupkg(rel3.Output);
        testHelper.LogResult($"Release build 3: nupkg num={relNum3}");

        if (relNum2 is not null && relNum3 is not null)
        {
            Assert.IsTrue(relNum3 > relNum2,
                $"Release should continue incrementing: rel2={relNum2}, rel3={relNum3}");
        }

        // ── One more Debug to confirm ──
        testHelper.LogStep("Debug again – still BuildNum 0");
        var dbg2 = testHelper.Build("Debug");
        Assert.AreEqual(0, dbg2.ExitCode, $"Debug build 2 failed.\n{dbg2.Error}");
        int? dbgNum2 = testHelper.ExtractBuildNumFromNupkg(dbg2.Output);
        testHelper.LogResult($"Debug build 2: nupkg num={dbgNum2}");

        if (dbgNum2 is not null)
        {
            Assert.AreEqual(0, dbgNum2.Value,
                $"Debug build should still use BuildNum 0 but got {dbgNum2}");
        }

        // ── One more Release ──
        testHelper.LogStep("Release again – still incrementing");
        var rel4 = testHelper.Build("Release");
        Assert.AreEqual(0, rel4.ExitCode);
        int? relNum4 = testHelper.ExtractBuildNumFromNupkg(rel4.Output);
        testHelper.LogResult($"Release build 4: nupkg num={relNum4}");

        if (relNum3 is not null && relNum4 is not null)
        {
            Assert.IsTrue(relNum4 > relNum3,
                $"Release should keep incrementing: rel3={relNum3}, rel4={relNum4}");
        }

        testHelper.LogResult("PASS – Conditionals: Release increments, Debug uses 0, swapping works");
    }
}
