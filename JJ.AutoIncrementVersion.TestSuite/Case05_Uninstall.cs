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
        testHelper.InitInstalledState();

        IsTrue(testHelper.CsprojHasPackageReference());

        testHelper.Rebuild();

        int buildNumBefore = testHelper.GetBuildNumFromXml();
        testHelper.LogResult($"BuildNum before uninstall: {buildNumBefore}");

        testHelper.UninstallPackage();
        IsFalse(testHelper.CsprojHasPackageReference());

        IsTrue(testHelper.BuildNumXmlExists());
        IsTrue(testHelper.DirectoryBuildPropsExists());

        // ── Build should succeed ──
        string buildOutput = testHelper.Rebuild();

        // ── Version should be frozen ──
        int buildNum1 = testHelper.GetBuildNumFromXml();
        string buildOutput2 = testHelper.Rebuild();
        int buildNum2 = testHelper.GetBuildNumFromXml();

        IsTrue(buildNum1 == buildNum2);
    }
}