namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case02_BuildWithoutPackage : TestBase
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
    public void Case02_BuildWithoutPackage_ProducesVersionEndingWithZero()
    {
        LogTitle("Initialize");
        {
            InitUninstalled();
            IsFalse(DirPropsExists());
            IsFalse(BuildNumXmlExists());
            IsFalse(CsprojHasPackageReference());
        }

        LogTitle("Version Stays .0");
        {
            string buildOutput = Rebuild();
            string packageFileName = ExtractPackageFileName(buildOutput);
            IsNotNull(packageFileName);
            IsTrue(packageFileName.EndsWith(".0.nupkg"));
        }
        {
            string buildOutput = Rebuild();
            string packageFileName = ExtractPackageFileName(buildOutput);
            IsNotNull(packageFileName);
            IsTrue(packageFileName.EndsWith(".0.nupkg"));
        }
    }
}
