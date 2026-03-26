namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case03_Install : TestBase
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
        LogTitle("Initialize");
        {
            InitUninstalled();
        }

        LogTitle("Install");
        {
            InstallPackage();
            IsTrue(CsprojHasPackageReference());
            IsFalse(BuildNumXmlExists());
            IsFalse(DirPropsExists());
        }

        LogTitle("First Build");
        {
            string output = Rebuild();

            IsTrue(BuildNumXmlExists());
            string buildNumContent = ReadBuildNumXml();
            IsTrue(buildNumContent.Contains("<BuildNum>"));

            IsTrue(DirPropsExists());
            string dirPropsContent = ReadDirProps();
            IsTrue(dirPropsContent.Contains("Import Project=\"BuildNum.xml\""));

            string packageFileName = ExtractPackageFileName(output);
            IsTrue(packageFileName.EndsWith(".0.nupkg"));
        }

        LogTitle("Increments Not Used Yet");
        {
            Rebuild_IncrementsXml_ButNotOutput(1);
            Rebuild_IncrementsXml_ButNotOutput(2);
            Rebuild_IncrementsXml_ButNotOutput(3);
        }
    }

    private void Rebuild_IncrementsXml_ButNotOutput(int expectedBuildNum)
    {
        int buildNum = GetBuildNumFromXml();
        IsTrue(buildNum == expectedBuildNum);
        string output = Rebuild();
        string packageName = ExtractPackageFileName(output);
        IsTrue(packageName.EndsWith(".0.nupkg"));
        Log();
    }
}