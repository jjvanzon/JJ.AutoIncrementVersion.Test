namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case05_Uninstall
{
    /// <summary>
    /// Manual Test Plan → "Uninstall"
    ///
    /// Steps:
    ///   1. Start from a working state (package installed, $(BuildNum) in version).
    ///   2. Uninstall package.
    ///   3. .xml and .Build.props should remain.
    ///   4. Build should succeed.
    ///   5. Version should stay frozen (no increment).
    /// </summary>
    [TestMethod]
    public void Case05_Uninstall_FilesRemainAndVersionFreezes()
    {
        using var testHelper = new TestHelper();

        testHelper.InitInstalledState();
        IsTrue(testHelper.BuildNumXmlExists());
        IsTrue(testHelper.DirPropsExists());
        IsTrue(testHelper.CsprojHasPackageReference());
        
        testHelper.GetBuildNumFromXml();
        string outputInit = testHelper.Rebuild();
        testHelper.ExtractPackageFileName(outputInit);
        // Don't assert: After 1st build BuildNum still increments
        
        testHelper.UninstallPackage();
        IsFalse(testHelper.CsprojHasPackageReference());
        IsTrue(testHelper.BuildNumXmlExists());
        IsTrue(testHelper.DirPropsExists());
        
        int buildNum1 = testHelper.GetBuildNumFromXml();
        string output1 = testHelper.Rebuild();
        string packageName1 = testHelper.ExtractPackageFileName(output1);
        IsTrue(packageName1.EndsWith($".{buildNum1}.nupkg"));
        
        int buildNum2 = testHelper.GetBuildNumFromXml();
        string output2 = testHelper.Rebuild();
        string packageName2 = testHelper.ExtractPackageFileName(output2);
        IsTrue(packageName2.EndsWith($".{buildNum2}.nupkg"));
            
        IsTrue(buildNum1 == buildNum2);
        IsTrue(string.Equals(packageName1, packageName2));
    }
}