namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class CommandLineAndUpgradeTests
{
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
        using var testHelper = new TestHelper();

        testHelper.LogStep("Restore committed state");
        testHelper.RestoreAll();

        testHelper.LogStep("Build with /p:BuildNum=9999");
        var result = testHelper.BuildWithArgs("Release", "/p:BuildNum=9999");
        Assert.AreEqual(0, result.ExitCode, $"Build failed.\n{result.Error}");

        // ── Verify output contains 9999 ──
        testHelper.LogStep("Verify nupkg ends with .9999.nupkg");
        string? nupkg = testHelper.ExtractNupkgName(result.Output);
        testHelper.LogResult($"Nupkg: {nupkg ?? "(none)"}");

        Assert.IsTrue(
            testHelper.OutputContainsNupkgEndingWith(result.Output, ".9999.nupkg"),
            $"Expected nupkg ending with .9999.nupkg but got: {nupkg}");

        // ── Verify BuildNum.xml saved 10000 ──
        testHelper.LogStep("Verify BuildNum.xml was updated to 10000");
        int savedNum = testHelper.GetBuildNumFromXml();
        testHelper.LogResult($"BuildNum in XML: {savedNum}");
        Assert.AreEqual(10000, savedNum,
            $"Expected BuildNum.xml to contain 10000 (9999+1) but got {savedNum}");

        testHelper.LogResult("PASS – Command Line Build: /p:BuildNum=9999 → .9999.nupkg, saved 10000");
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
        using var testHelper = new TestHelper();

        testHelper.LogStep("Restore committed state and build once");
        testHelper.RestoreAll();
        testHelper.Build();

        // ── Remove BuildNumWasFromXmljj ──
        testHelper.LogStep("Remove BuildNumWasFromXmljj from BuildNum.xml");
        string xml = testHelper.ReadBuildNumXml();
        testHelper.LogResult($"Before: {xml.Trim()}");

        // TODO: Assert it was in there before.

        string modified = xml.Replace(
            "<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>", "");
        testHelper.WriteBuildNumXml(modified);
        testHelper.LogResult($"After removal: {testHelper.ReadBuildNumXml().Trim()}");

        Assert.IsFalse(testHelper.ReadBuildNumXml().Contains("BuildNumWasFromXmljj"),
            "BuildNumWasFromXmljj should have been removed.");

        // ── Build ──
        testHelper.LogStep("Build – should restore BuildNumWasFromXmljj");
        var build1 = testHelper.Build();
        // TODO: Error is not in build1.Error It's embedded in build1.Output.
        Assert.AreEqual(0, build1.ExitCode, $"Build failed.\n{build1.Error}");

        testHelper.LogStep("Verify BuildNumWasFromXmljj was restored");
        string afterBuild = testHelper.ReadBuildNumXml();
        testHelper.LogResult($"BuildNum.xml after build: {afterBuild.Trim()}");
        Assert.IsTrue(afterBuild.Contains("BuildNumWasFromXmljj"),
            "BuildNumWasFromXmljj should be restored after build.");

        // ── Verify continued increment ──
        testHelper.LogStep("Verify continued increment");
        int? prev = testHelper.ExtractBuildNumFromNupkg(build1.Output);
        for (int i = 0; i < 2; i++)
        {
            var next = testHelper.Build();
            Assert.AreEqual(0, next.ExitCode, $"Build {i + 2} failed.\n{next.Error}");

            int? cur = testHelper.ExtractBuildNumFromNupkg(next.Output);
            testHelper.LogResult($"Build {i + 2}: nupkg num={cur}");

            if (prev is not null && cur is not null)
            {
                Assert.IsTrue(cur > prev,
                    $"Expected increment: prev={prev}, cur={cur}");
            }
            prev = cur;
        }

        testHelper.LogResult("PASS – Upgrade Regression: BuildNumWasFromXmljj restored, increments continue");
    }
}
