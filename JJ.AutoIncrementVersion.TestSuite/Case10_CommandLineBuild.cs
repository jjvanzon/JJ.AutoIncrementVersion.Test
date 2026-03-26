namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case10_CommandLineBuild : TestBase
{
    /// <summary>
    /// Manual Test Plan → "Command Line Build"
    ///
    /// Steps:
    ///   1. Adding /p:BuildNum=9999 to dotnet build outputs package ending with 9999.
    ///   2. It saved 9999 + 1 = 10000 back to BuildNum.xml.
    /// </summary>
    [TestMethod]
    public void Case10_CommandLineBuild_OverridesBuildNumAndSavesNext()
    {
        InitInstalledState();
        Log();

        GetBuildNumFromXml();
        string buildOutput = Rebuild("/p:BuildNum=9999");
        string packageName = ExtractPackageFileName(buildOutput);
        IsTrue(packageName.EndsWith(".9999.nupkg"));
        int savedNum = GetBuildNumFromXml();
        AreEqual(10000, savedNum);
    }
}