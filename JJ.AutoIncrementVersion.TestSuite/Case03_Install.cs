namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case03_Install : TestHelper
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
        InitUninstalled();
        Log();

        InstallPackage();
        Log();

        string buildOutput = Rebuild();

        IsTrue(BuildNumXmlExists());
        string buildNumContent = ReadBuildNumXml();
        IsTrue(buildNumContent.Contains("<BuildNum>"));
        IsTrue(buildNumContent.Contains("<DisableFastUpToDateCheck>True</DisableFastUpToDateCheck>"));
        IsTrue(buildNumContent.Contains("<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>"));

        IsTrue(DirPropsExists());
        string dirPropsContent = ReadDirProps();
        IsTrue(dirPropsContent.Contains("<BuildNum>0</BuildNum>"));
        IsTrue(dirPropsContent.Contains("Import Project=\"BuildNum.xml\""));

        string packageFileName = ExtractPackageFileName(buildOutput);
        IsTrue(packageFileName.EndsWith(".0.nupkg"));
        Log();
    }
}
