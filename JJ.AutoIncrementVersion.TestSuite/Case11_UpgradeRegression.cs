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
    public void Case11_UpgradeRegression_RestoresBuildNumWasFromXmljjAndIncrements()
    {
        using var testHelper = new TestHelper();

        testHelper.LogStep("Restore committed state and build once");
        testHelper.InitInstalledState();
        testHelper.Rebuild();

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
        var buildOutput1 = testHelper.Rebuild();

        string buildNumAfterBuild = testHelper.ReadBuildNumXml();
        IsTrue(buildNumAfterBuild.Contains("BuildNumWasFromXmljj"));

        // ── Verify continued increment ──
        testHelper.LogStep("Verify continued increment");
        int? previousBuildNum = testHelper.ExtractBuildNumFromNupkgName(buildOutput1);
        for (int i = 0; i < 2; i++)
        {
            string nextBuildOutput = testHelper.Rebuild();
            int? nextBuildNum = testHelper.ExtractBuildNumFromNupkgName(nextBuildOutput);

            // TODO: Assert exact increments by 1.
            if (previousBuildNum is not null && nextBuildNum is not null)
            {
                IsTrue(nextBuildNum > previousBuildNum);
            }
            previousBuildNum = nextBuildNum;
        }
    }
}