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
            RebuildsWith(buildNum: 1, packNum: 0);
            RebuildsWith(buildNum: 2, packNum: 0);
            RebuildsWith(buildNum: 3, packNum: 0);
        }
    }
}