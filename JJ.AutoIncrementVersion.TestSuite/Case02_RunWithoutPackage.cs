namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case02_RunWithoutPackage : TestBase
{
    /// <summary>
    /// Manual Test Plan → "Set Initial State" + "Run Without Package"
    /// 
    /// Steps:
    ///   1. Uninstall existing JJ.AutoIncrementVersion package.
    ///   2. Delete BuildNum.xml and Directory.Build.props.
    ///   3. Replace $(BuildNum) with 0 in csproj.
    ///   4. Rebuild solution (= rebuild the project under test).
    ///   5. Output shows .0.nupkg
    /// </summary>
    [TestMethod]
    public void Case02_RunWithoutPackage_ProducesVersionEndingWithZero()
    {
        InitUninstalled();
        Log();
        {
            string buildOutput = Rebuild();
            string packageFileName = ExtractPackageFileName(buildOutput);
            IsNotNull(packageFileName);
            IsTrue(packageFileName.EndsWith(".0.nupkg"));
            Log();
        }
        {
            string buildOutput = Rebuild();
            string packageFileName = ExtractPackageFileName(buildOutput);
            IsNotNull(packageFileName);
            IsTrue(packageFileName.EndsWith(".0.nupkg"));
            Log();
        }
    }
}
