namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case05_Uninstall : TestBase
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
        LogTitle("Initialize");
        {
            InitInstalledState();
            IsTrue(BuildNumXmlExists());
            IsTrue(DirPropsExists());
            IsTrue(CsprojHasPackageReference());
        }

        LogTitle("Verify Working State");
        {
            RebuildsIncrement();
        }

        LogTitle("Uninstall");
        {
            UninstallPackage();
            IsFalse(CsprojHasPackageReference());
        }
        
        LogTitle("Files Remain");
        {
            IsTrue(BuildNumXmlExists());
            IsTrue(DirPropsExists());
        }
        
        LogTitle("Version Freezes");
        {
            RebuildsFreezeVersion();
        }
    }
}