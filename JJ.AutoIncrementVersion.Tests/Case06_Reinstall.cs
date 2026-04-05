namespace JJ.AutoIncrementVersion.Tests;

[TestClass]
public class Case06_Reinstall : TestBase
{
    // TODO: One test has one step. This test has 2. Split. But also: all tests use same dependency, so can't run in parallel. Enforce that.
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
            Log();
            IsTrue(BuildNumXmlExists());
            IsTrue(DirPropsExists());
            IsTrue(CsprojHasPackageReference());
            Log();
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