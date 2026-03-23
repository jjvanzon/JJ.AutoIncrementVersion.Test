namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case10_CommandLineBuild
{
    /// <summary>
    /// Manual Test Plan → "Command Line Build"
    ///
    /// Steps:
    ///   1. Adding /p:BuildNum=9999 to dotnet build outputs package ending with 9999.
    ///   2. It saved 9999 + 1 = 10000 back to BuildNum.xml.
    /// </summary>
    [TestMethod]
    public void Case10_CommandLineBuild_OverridesBuildNumAndSavesNext()
    {
        using var testHelper = new TestHelper();

        testHelper.SetInstalledState();

        string buildOutput = testHelper.RebuildWithArgs("/p:BuildNum=9999");

        // ── Verify output contains 9999 ──
        testHelper.LogStep("Verify nupkg ends with .9999.nupkg");
        string? nupkg = testHelper.ExtractNupkgName(buildOutput);
        // TODO: Dpn't just log. Assert.
        testHelper.LogResult($"Nupkg: {nupkg ?? "(none)"}");

        IsTrue(testHelper.OutputContainsNupkgEndingWith(buildOutput, ".9999.nupkg"));

        // ── Verify BuildNum.xml saved 10000 ──
        int savedNum = testHelper.GetBuildNumFromXml();
        AreEqual(10000, savedNum);
    }
}