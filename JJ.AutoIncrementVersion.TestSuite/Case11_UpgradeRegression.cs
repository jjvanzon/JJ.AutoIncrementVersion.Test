namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case11_UpgradeRegression
{
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
        testHelper.SetInstalledState();
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

        IsFalse(testHelper.ReadBuildNumXml().Contains("BuildNumWasFromXmljj"));

        // ── Build ──
        testHelper.LogStep("Build – should restore BuildNumWasFromXmljj");
        var build1 = testHelper.Build();
        // TODO: Error is not in build1.Error It's embedded in build1.Output.
        AreEqual(0, build1.ExitCode, $"Build failed.\n{build1.Error}");

        string buildNumAfterBuild = testHelper.ReadBuildNumXml();
        IsTrue(buildNumAfterBuild.Contains("BuildNumWasFromXmljj"));

        // ── Verify continued increment ──
        testHelper.LogStep("Verify continued increment");
        int? previousBuildNum = testHelper.ExtractBuildNumFromNupkgName(build1.Output);
        for (int i = 0; i < 2; i++)
        {
            CommandLineResult nextBuildResult = testHelper.Build();
            AreEqual(0, nextBuildResult.ExitCode, $"Build {i + 2} failed.\n{nextBuildResult.Error}");

            int? currentBuildNum = testHelper.ExtractBuildNumFromNupkgName(nextBuildResult.Output);
            testHelper.LogResult($"Build {i + 2}: nupkg num={currentBuildNum}");

            if (previousBuildNum is not null && currentBuildNum is not null)
            {
                IsTrue(currentBuildNum > previousBuildNum);
            }
            previousBuildNum = currentBuildNum;
        }

        testHelper.LogResult("PASS – Upgrade Regression: BuildNumWasFromXmljj restored, increments continue");
    }
}