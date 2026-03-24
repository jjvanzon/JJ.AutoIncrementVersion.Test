
namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case04_FirstUse
{
    /// <summary>
    /// Manual Test Plan → "First Use"
    ///
    /// Steps:
    ///   1. Prepare Initial State.
    ///   2. Install JJ.AutoIncrementVersion package.
    ///   3. Set csproj Version to 4.3.$(BuildNum).
    ///   4. 1st rebuild should fail ("Invalid NuGet version string: '4.3.'").
    ///   5. 2nd build succeeds.
    ///   6. Auto-creates BuildNum.xml and Directory.Build.props.
    ///   7. Output shows .0.nupkg.
    ///   8. Subsequent builds auto-increment (.1, .2, …).
    /// </summary>
    [TestMethod]
    public void Case04_FirstUse_FailsThenSucceedsThenIncrements()
    {
        using var testHelper = new TestHelper();

        testHelper.InitUninstalled();
        testHelper.InstallPackage();
        testHelper.SetCsprojVersion("4.3.$(BuildNum)");
        testHelper.RebuildExpectFail();

        IsFalse(testHelper.BuildNumXmlExists());
        IsTrue(testHelper.DirPropsExists()); // There anyway

        string buildOutput0 = testHelper.Rebuild();

        IsTrue(testHelper.BuildNumXmlExists());
        IsTrue(testHelper.DirPropsExists());

        string packageName0 = testHelper.ExtractPackageFileName(buildOutput0);
        IsTrue(packageName0.EndsWith(".0.nupkg"));

        int xmlBuildNum1 = testHelper.GetBuildNumFromXml();
        AreEqual(1, xmlBuildNum1);
        string buildOutput1 = testHelper.Rebuild();
        string packageName1 = testHelper.ExtractPackageFileName(buildOutput1);
        IsTrue(packageName1.EndsWith(".1.nupkg"));

        int xmlBuildNum2 = testHelper.GetBuildNumFromXml();
        AreEqual(2, xmlBuildNum2);
        string buildOutput2 = testHelper.Rebuild();
        string packageName2 = testHelper.ExtractPackageFileName(buildOutput2);
        IsTrue(packageName2.EndsWith(".2.nupkg"));
    }
}
