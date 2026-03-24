namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case06_Reinstall : TestHelper
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
        InitInstalledState();
        IsTrue(BuildNumXmlExists());
        IsTrue(DirPropsExists());
        IsTrue(CsprojHasPackageReference());
        GetBuildNumFromXml();
        Rebuild();
        GetBuildNumFromXml();
        Log();

        UninstallPackage();
        IsFalse(CsprojHasPackageReference());
        IsTrue(BuildNumXmlExists());
        IsTrue(DirPropsExists());
        GetBuildNumFromXml();
        Rebuild();
        GetBuildNumFromXml();
        Log();
        
        InstallPackage();
        Log();

        RebuildsIncrement();
    }
}