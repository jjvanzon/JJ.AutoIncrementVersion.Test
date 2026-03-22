using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class CommandLineAndUpgradeTests
{
    private TestHelper _h = null!;

    [TestInitialize]
    public void Init() => _h = new TestHelper(TestContext); // TODO: Init and CleanUp may as well be put in the test code iteself.

    [TestCleanup]
    public void Cleanup() => _h.Cleanup();

    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Manual Test Plan → "Command Line Build"
    ///
    /// Steps:
    ///   1. Adding /p:BuildNum=9999 to dotnet build outputs package ending with 9999.
    ///   2. It saved 9999 + 1 = 10000 back to BuildNum.xml.
    /// </summary>
    [TestMethod]
    public void CommandLineBuild_OverridesBuildNumAndSavesNext()
    {
        _h.LogStep("Restore committed state");
        _h.GitRestoreAll();

        _h.LogStep("Build with /p:BuildNum=9999");
        var result = _h.Build("Release", "/p:BuildNum=9999");
        Assert.AreEqual(0, result.ExitCode, $"Build failed.\n{result.Error}");

        // ── Verify output contains 9999 ──
        _h.LogStep("Verify nupkg ends with .9999.nupkg");
        string? nupkg = _h.ExtractNupkgName(result.Output);
        _h.LogResult($"Nupkg: {nupkg ?? "(none)"}");

        Assert.IsTrue(
            _h.OutputContainsNupkgEndingWith(result.Output, ".9999.nupkg"),
            $"Expected nupkg ending with .9999.nupkg but got: {nupkg}");

        // ── Verify BuildNum.xml saved 10000 ──
        _h.LogStep("Verify BuildNum.xml was updated to 10000");
        int savedNum = _h.GetBuildNumFromXml();
        _h.LogResult($"BuildNum in XML: {savedNum}");
        Assert.AreEqual(10000, savedNum,
            $"Expected BuildNum.xml to contain 10000 (9999+1) but got {savedNum}");

        _h.LogResult("PASS – Command Line Build: /p:BuildNum=9999 → .9999.nupkg, saved 10000");
    }

    /// <summary>
    /// Manual Test Plan → "Upgrade Regression"
    ///
    /// Steps:
    ///   1. Remove BuildNumWasFromXmljj from BuildNum.xml (simulating upgrade path).
    ///   2. Build – should restore BuildNumWasFromXmljj.
    ///   3. Continues to increment build numbers.
    /// </summary>
    [TestMethod]
    public void UpgradeRegression_RestoresBuildNumWasFromXmljjAndIncrements()
    {
        _h.LogStep("Restore committed state and build once");
        _h.GitRestoreAll(); // Ensure initial state otherwise.
        _h.Build();

        // ── Remove BuildNumWasFromXmljj ──
        _h.LogStep("Remove BuildNumWasFromXmljj from BuildNum.xml");
        string xml = _h.ReadBuildNumXml();
        _h.LogResult($"Before: {xml.Trim()}");

        // TODO: Assert it was in there before.

        string modified = xml.Replace(
            "<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>", "");
        _h.WriteBuildNumXml(modified);
        _h.LogResult($"After removal: {_h.ReadBuildNumXml().Trim()}");

        Assert.IsFalse(_h.ReadBuildNumXml().Contains("BuildNumWasFromXmljj"),
            "BuildNumWasFromXmljj should have been removed.");

        // ── Build ──
        _h.LogStep("Build – should restore BuildNumWasFromXmljj");
        var build1 = _h.Build();
        // TODO: Error is not in build1.Error It's embedded in build1.Output.
        Assert.AreEqual(0, build1.ExitCode, $"Build failed.\n{build1.Error}");

        _h.LogStep("Verify BuildNumWasFromXmljj was restored");
        string afterBuild = _h.ReadBuildNumXml();
        _h.LogResult($"BuildNum.xml after build: {afterBuild.Trim()}");
        Assert.IsTrue(afterBuild.Contains("BuildNumWasFromXmljj"),
            "BuildNumWasFromXmljj should be restored after build.");

        // ── Verify continued increment ──
        _h.LogStep("Verify continued increment");
        int? prev = _h.ExtractBuildNumFromNupkg(build1.Output);
        for (int i = 0; i < 2; i++)
        {
            var next = _h.Build();
            Assert.AreEqual(0, next.ExitCode, $"Build {i + 2} failed.\n{next.Error}");

            int? cur = _h.ExtractBuildNumFromNupkg(next.Output);
            _h.LogResult($"Build {i + 2}: nupkg num={cur}");

            if (prev is not null && cur is not null)
            {
                Assert.IsTrue(cur > prev,
                    $"Expected increment: prev={prev}, cur={cur}");
            }
            prev = cur;
        }

        _h.LogResult("PASS – Upgrade Regression: BuildNumWasFromXmljj restored, increments continue");
    }
}
