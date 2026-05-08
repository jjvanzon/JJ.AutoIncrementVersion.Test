namespace JJ.AutoIncrementVersion.Tests;

[TestClass]
public class Case06_Reinstall : TestBase
{
    /// <summary>
    /// Manual Test Plan → "Reinstall"
    ///
    /// Steps:
    ///   1. Uninstall, then reinstall the package.
    ///   2. Build should succeed, incrementing version each time.
    /// </summary>
    [TestMethod]
    public void Case06_Reinstall_BuildSucceedsAndIncrements()
    {
        LogTitle("Verify Installed State");
        {
            InitInstalledState();
            LogLine();
            IsTrue(BuildNumXmlExists());
            IsTrue(DirPropsExists());
            IsTrue(CsprojHasPackageReference());
            LogLine();
            RebuildsIncrement(repeats: 2);
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
            RebuildsFreezeVersion(repeats: 2);
        }

        LogTitle("Reinstall");
        {
            InstallPackage();
        }

        LogTitle("Rebuilds Increment Again");
        { 
            RebuildsIncrement();
        }
    }
}