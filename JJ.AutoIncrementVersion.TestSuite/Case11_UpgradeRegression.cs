namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
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
        LogTitle("Initialize");
        {
            InitInstalledState();
            IsTrue(BuildNumXmlExists());
            IsTrue(DirPropsExists());
        }

        LogTitle("Verify Working State");
        {
            RebuildsIncrement(repeats: 2);
            string xml = ReadBuildNumXml();
            IsTrue(xml.Contains("BuildNumWasFromXmljj"));
            Log($"BuildNum.xml = {xml.RemoveExcessiveWhiteSpace()}");
        }
        LogTitle("Remove Control Flag");
        {
            Log("Might influence behavior considerably.");
            string xml = ReadBuildNumXml();
            IsTrue(xml.Contains("BuildNumWasFromXmljj"));
            string modified = xml.Replace("<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>", "");
            WriteBuildNumXml(modified);
            string readBack = ReadBuildNumXml();
            IsFalse(readBack.Contains("BuildNumWasFromXmljj"));
            Log("BuildNumWasFromXmljj flag removed.");
            Log($"BuildNum.xml = {readBack.RemoveExcessiveWhiteSpace()}");
        }
        LogTitle("Verify Flag Restores");
        {
            int buildNum = GetBuildNumFromXml();
            string output = Rebuild();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith($".{buildNum}.nupkg"));
            string readBack = ReadBuildNumXml();
            IsTrue(readBack.Contains("BuildNumWasFromXmljj"));
            
            Log();
            Log("BuildNumWasFromXmljj flag restored.");
            Log($"BuildNum.xml = {readBack.RemoveExcessiveWhiteSpace()}");
        }
        LogTitle("Verify Continued Working");
        {
            RebuildsIncrement(repeats: 2);
        }
    }
}