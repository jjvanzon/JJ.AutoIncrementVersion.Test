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

        int buildNum1 = GetBuildNumFromXml();
        string output1 = Rebuild();
        string packageName1 = ExtractPackageFileName(output1);
        IsTrue(packageName1.EndsWith($".{buildNum1}.nupkg"));
        Log();

        int buildNum2 = GetBuildNumFromXml();
        string output2 = Rebuild();
        string packageName2 = ExtractPackageFileName(output2);
        IsTrue(packageName2.EndsWith($".{buildNum2}.nupkg"));
        AreEqual(buildNum1 + 1, buildNum2);
        Log();

        int buildNum3 = GetBuildNumFromXml();
        string output3 = Rebuild();
        string packageName3 = ExtractPackageFileName(output3);
        IsTrue(packageName3.EndsWith($".{buildNum3}.nupkg"));
        AreEqual(buildNum2 + 1, buildNum3);
        Log();
    }
}