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
    public void CommandLineBuild_OverridesBuildNumAndSavesNext()
    {
        using var testHelper = new TestHelper();

        testHelper.SetInstalledState();

        testHelper.LogStep("Build with /p:BuildNum=9999");
        var buildResult = testHelper.BuildWithArgs("Release", "/p:BuildNum=9999");
        AreEqual(0, buildResult.ExitCode);

        // ── Verify output contains 9999 ──
        testHelper.LogStep("Verify nupkg ends with .9999.nupkg");
        string? nupkg = testHelper.ExtractNupkgName(buildResult.Output);
        testHelper.LogResult($"Nupkg: {nupkg ?? "(none)"}");

        IsTrue(
            testHelper.OutputContainsNupkgEndingWith(buildResult.Output, ".9999.nupkg"),
            $"Expected nupkg ending with .9999.nupkg but got: {nupkg}");

        // ── Verify BuildNum.xml saved 10000 ──
        testHelper.LogStep("Verify BuildNum.xml was updated to 10000");
        int savedNum = testHelper.GetBuildNumFromXml();
        testHelper.LogResult($"BuildNum in XML: {savedNum}");
        AreEqual(10000, savedNum,  $"Expected BuildNum.xml to contain 10000 (9999+1) but got {savedNum}");

        testHelper.LogResult("PASS – Command Line Build: /p:BuildNum=9999 → .9999.nupkg, saved 10000");
    }
}