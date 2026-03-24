namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case05_Uninstall : TestHelper
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
        InitInstalledState();
        IsTrue(BuildNumXmlExists());
        IsTrue(DirPropsExists());
        IsTrue(CsprojHasPackageReference());
        Log();
        
        GetBuildNumFromXml(); // Logs BuildNum
        string outputInit = Rebuild();
        ExtractPackageFileName(outputInit); // Logs package name
        // Don't assert equals: After 1st build BuildNum increments
        Log();
        
        UninstallPackage();
        IsFalse(CsprojHasPackageReference());
        IsTrue(BuildNumXmlExists());
        IsTrue(DirPropsExists());
        Log();
        
        int buildNum1 = GetBuildNumFromXml();
        string output1 = Rebuild();
        string packageName1 = ExtractPackageFileName(output1);
        IsTrue(packageName1.EndsWith($".{buildNum1}.nupkg"));
        Log();
        
        int buildNum2 = GetBuildNumFromXml();
        string output2 = Rebuild();
        string packageName2 = ExtractPackageFileName(output2);
        IsTrue(packageName2.EndsWith($".{buildNum2}.nupkg"));
        Log();
            
        IsTrue(buildNum1 == buildNum2);
        IsTrue(string.Equals(packageName1, packageName2));
        Log();
    }
}