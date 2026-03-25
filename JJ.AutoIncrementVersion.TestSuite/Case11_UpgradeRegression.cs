namespace JJ.AutoIncrementVersion.TestSuite;

//[TestClass]
public class Case11_UpgradeRegression : TestBase
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
        {
            InitInstalledState();
            Log("UpgradeRegression: simulate old BuildNum.xml without BuildNumWasFromXmljj.");
            IsTrue(BuildNumXmlExists());
            IsTrue(DirPropsExists());
            GetBuildNumFromXml();
            string output = Rebuild();
            ExtractPackageFileName(output);
            GetBuildNumFromXml();
            Log();
        }
        {
            Log("Remove BuildNumWasFromXmljj from BuildNum.xml.");
            string xml = ReadBuildNumXml();
            IsTrue(xml.Contains("BuildNumWasFromXmljj"));
            string modified = xml.Replace("<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>", "");
            WriteBuildNumXml(modified);
            IsFalse(ReadBuildNumXml().Contains("BuildNumWasFromXmljj"));
            GetBuildNumFromXml();
            Log();
        }
        {
            Log("Build should restore BuildNumWasFromXmljj.");
            int buildNum = GetBuildNumFromXml();
            string output = Rebuild();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith($".{buildNum}.nupkg"));
            IsTrue(ReadBuildNumXml().Contains("BuildNumWasFromXmljj"));
            GetBuildNumFromXml();
            Log();
        }
        {
            Log("After restore, builds should keep incrementing.");
            RebuildsIncrement();
        }
    }
}