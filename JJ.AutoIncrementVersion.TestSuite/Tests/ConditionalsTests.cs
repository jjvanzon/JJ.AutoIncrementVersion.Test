using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class ConditionalsTests
{
    private TestHelper _h = null!;

    [TestInitialize]
    public void Init() => _h = new TestHelper(TestContext); // TODO: Init and CleanUp may as well be put in the test code iteself.

    [TestCleanup]
    public void Cleanup() => _h.Cleanup();

    public TestContext TestContext { get; set; } = null!;

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
        // ── Restore committed state (which already has the conditional) ──
        _h.LogStep("Restore committed state");
        _h.GitRestoreAll(); // Git reset mid-test is a no. Either set up initial state explicitly, or follow a sequential plan to get to that state in the formerly manual test. By the way: tests can't run in parallel which they are assumed to be able to, so they have to be forced to be sequential.

        // ── Verify Directory.Build.props has the conditional ──
        _h.LogStep("Verify Directory.Build.props has Release condition");
        string propsContent = _h.ReadDirectoryBuildProps();
        _h.LogResult($"Directory.Build.props: {propsContent.Trim()}");
        // Assert can fail because the Release condition is not guaranteed as an initial state.
        Assert.IsTrue(
            propsContent.Contains("$(Configuration)=='Release'", StringComparison.OrdinalIgnoreCase),
            "Directory.Build.props should have the Release condition.");

        // ── Release build – should increment ──
        _h.LogStep("Release build 1");
        var rel1 = _h.Build("Release");
        Assert.AreEqual(0, rel1.ExitCode, $"Release build 1 failed.\n{rel1.Error}");
        int? relNum1 = _h.ExtractBuildNumFromNupkg(rel1.Output);
        _h.LogResult($"Release build 1: nupkg num={relNum1}");

        _h.LogStep("Release build 2");
        var rel2 = _h.Build("Release");
        Assert.AreEqual(0, rel2.ExitCode, $"Release build 2 failed.\n{rel2.Error}");
        int? relNum2 = _h.ExtractBuildNumFromNupkg(rel2.Output);
        _h.LogResult($"Release build 2: nupkg num={relNum2}");

        if (relNum1 is not null && relNum2 is not null)
        {
            Assert.IsTrue(relNum2 > relNum1,
                $"Release should increment: rel1={relNum1}, rel2={relNum2}");
        }

        // ── Debug build – should use BuildNum 0 ──
        _h.LogStep("Debug build – should use BuildNum 0");
        var dbg = _h.Build("Debug");
        Assert.AreEqual(0, dbg.ExitCode, $"Debug build failed.\n{dbg.Error}");

        int? dbgNum = _h.ExtractBuildNumFromNupkg(dbg.Output);
        _h.LogResult($"Debug build: nupkg num={dbgNum}");

        // Debug build may or may not produce a nupkg depending on GeneratePackageOnBuild,
        // but if it does, the BuildNum portion should be 0.
        if (dbgNum is not null)
        {
            Assert.AreEqual(0, dbgNum.Value,
                $"Debug build should use BuildNum 0 but got {dbgNum}");
        }
        else
        {
            _h.LogWarning("Debug build did not produce a nupkg name in output – checking not possible.");
        }

        // ── Swap back to Release – should continue incrementing ──
        _h.LogStep("Back to Release – should continue from where it left off");
        var rel3 = _h.Build("Release");
        Assert.AreEqual(0, rel3.ExitCode, $"Release build 3 failed.\n{rel3.Error}");
        int? relNum3 = _h.ExtractBuildNumFromNupkg(rel3.Output);
        _h.LogResult($"Release build 3: nupkg num={relNum3}");

        if (relNum2 is not null && relNum3 is not null)
        {
            Assert.IsTrue(relNum3 > relNum2,
                $"Release should continue incrementing: rel2={relNum2}, rel3={relNum3}");
        }

        // ── One more Debug to confirm ──
        _h.LogStep("Debug again – still BuildNum 0");
        var dbg2 = _h.Build("Debug");
        Assert.AreEqual(0, dbg2.ExitCode, $"Debug build 2 failed.\n{dbg2.Error}");
        int? dbgNum2 = _h.ExtractBuildNumFromNupkg(dbg2.Output);
        _h.LogResult($"Debug build 2: nupkg num={dbgNum2}");

        if (dbgNum2 is not null)
        {
            Assert.AreEqual(0, dbgNum2.Value,
                $"Debug build should still use BuildNum 0 but got {dbgNum2}");
        }

        // ── One more Release ──
        _h.LogStep("Release again – still incrementing");
        var rel4 = _h.Build("Release");
        Assert.AreEqual(0, rel4.ExitCode);
        int? relNum4 = _h.ExtractBuildNumFromNupkg(rel4.Output);
        _h.LogResult($"Release build 4: nupkg num={relNum4}");

        if (relNum3 is not null && relNum4 is not null)
        {
            Assert.IsTrue(relNum4 > relNum3,
                $"Release should keep incrementing: rel3={relNum3}, rel4={relNum4}");
        }

        _h.LogResult("PASS – Conditionals: Release increments, Debug uses 0, swapping works");
    }
}
