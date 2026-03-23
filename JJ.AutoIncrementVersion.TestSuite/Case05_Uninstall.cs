namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case05_Uninstall
{
    /// <summary>
    /// Manual Test Plan → "Uninstall"
    ///
    /// Steps:
    ///   1. Start from a working state (package installed, $(BuildNum) in version).
    ///   2. Uninstall package.
    ///   3. .xml and .Build.props should remain.
    ///   4. Build should succeed.
    ///   5. Version should stay frozen (no increment).
    /// </summary>
    [TestMethod]
    public void Case05_Uninstall_FilesRemainAndVersionFreezes()
    {
        using var testHelper = new TestHelper();

        // ── Establish working state ──
        testHelper.LogStep("Establish working state with package installed");
        // The isolated folder already has the package reference and $(BuildNum) in version.
        // Do a build to ensure BuildNum.xml/props exist.
        testHelper.Build();

        int buildNumBefore = testHelper.GetBuildNumFromXml();
        testHelper.LogResult($"BuildNum before uninstall: {buildNumBefore}");

        // ── Uninstall ──
        testHelper.LogStep("Uninstall package");
        testHelper.UninstallPackage(); // TODO: Failure is ignored, which is not good.
        testHelper.LogResult($"Package uninstalled. Has reference: {testHelper.CsprojHasPackageReference()}");

        // ── Verify files remain ──
        IsTrue(testHelper.BuildNumXmlExists());
        IsTrue(testHelper.DirectoryBuildPropsExists());

        // ── Build should succeed ──
        testHelper.LogStep("Build after uninstall – should succeed");
        var buildResult = testHelper.Build();
        AreEqual(0, buildResult.ExitCode, $"Build failed after uninstall.\n{buildResult.Error}");
        testHelper.LogResult("Build succeeded.");

        // ── Version should be frozen ──
        testHelper.LogStep("Build again – version should stay frozen (no increment)");
        int buildNumAfterBuild = testHelper.GetBuildNumFromXml();
        var secondBuildResult = testHelper.Build();
        int buildNumAfterSecond = testHelper.GetBuildNumFromXml();
        testHelper.LogResult($"BuildNum after 1st build: {buildNumAfterBuild}, after 2nd build: {buildNumAfterSecond}");

        AreEqual(buildNumAfterBuild, buildNumAfterSecond, "BuildNum should not change when package is uninstalled.");

        testHelper.LogResult("PASS – Uninstall: files remain, build succeeds, version frozen");
    }
}