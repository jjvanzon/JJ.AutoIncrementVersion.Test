namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case06_Reinstall
{
    // TODO: One test has one step. This test has 2. Split. But also: all tests use same dependency, so can't run in parallel. Enforce that.
    /// <summary>
    /// Manual Test Plan → "Reinstall"
    ///
    /// Steps:
    ///   1. Uninstall, then reinstall the package.
    ///   2. Build should succeed, incrementing version each time.
    /// </summary>
    [TestMethod]
    public void Case06_Reinstall_BuildSucceedsAndIncrements()
    {
        using var testHelper = new TestHelper();

        testHelper.InitInstalledState();
        testHelper.Rebuild();
        testHelper.UninstallPackage();
        testHelper.InstallPackage();

        string buildOutput1 = testHelper.Rebuild();
        int buildNum1 = testHelper.ExtractPackageBuildNum(buildOutput1);

        string buildOutput2 = testHelper.Rebuild();
        int buildNum2 = testHelper.ExtractPackageBuildNum(buildOutput2);
        AreEqual(buildNum1 + 1, buildNum2);

        string buildOutput3 = testHelper.Rebuild();
        int buildNum3 = testHelper.ExtractPackageBuildNum(buildOutput3);
        AreEqual(buildNum2 + 1, buildNum3);
    }
}