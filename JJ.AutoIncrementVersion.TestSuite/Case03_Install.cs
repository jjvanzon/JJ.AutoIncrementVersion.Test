namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case03_Install
{
    /// <summary>
    /// Manual Test Plan → "Install"
    ///
    /// Steps:
    ///   1. Set Initial State.
    ///   2. Install JJ.AutoIncrementVersion package.
    ///   3. Rebuild – output shows .0.nupkg.
    ///   4. Auto-creates BuildNum.xml with expected content.
    ///   5. Auto-creates Directory.Build.props with expected content.
    /// </summary>
    [TestMethod]
    public void Case03_Install_AutoCreatesFilesAndBuildsWithZero()
    {
        using var testHelper = new TestHelper();
        testHelper.InitUninstalled();
        testHelper.InstallPackage();

        string buildOutput = testHelper.Rebuild();

        IsTrue(testHelper.BuildNumXmlExists());
        string buildNumContent = testHelper.ReadBuildNumXml();
        IsTrue(buildNumContent.Contains("<BuildNum>"));
        IsTrue(buildNumContent.Contains("<DisableFastUpToDateCheck>True</DisableFastUpToDateCheck>"));
        IsTrue(buildNumContent.Contains("<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>"));

        IsTrue(testHelper.DirectoryBuildPropsExists());
        string dirPropsContent = testHelper.ReadDirectoryBuildProps();
        IsTrue(dirPropsContent.Contains("<BuildNum>0</BuildNum>"));
        IsTrue(dirPropsContent.Contains("Import Project=\"BuildNum.xml\""));

        string packageFileName = testHelper.ExtractPackageFileName(buildOutput);
        IsTrue(packageFileName.EndsWith(".0.nupkg"));
    }
}
