
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

        {
            testHelper.InstallPackage();
            testHelper.SetProjPatchNum("$(BuildNum)");
            testHelper.RebuildExpectFail();
            IsFalse(testHelper.BuildNumXmlExists());
            IsTrue(testHelper.DirPropsExists()); // There anyway
        }
        {
            string output = testHelper.Rebuild();
            IsTrue(testHelper.BuildNumXmlExists());
            IsTrue(testHelper.DirPropsExists());
            string packageName = testHelper.ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith(".0.nupkg"));
        }
        {
            int buildNum = testHelper.GetBuildNumFromXml();
            AreEqual(1, buildNum);
            string output = testHelper.Rebuild();
            string packageName = testHelper.ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith(".1.nupkg"));
        }
        {
            int buildNum = testHelper.GetBuildNumFromXml();
            AreEqual(2, buildNum);
            string output = testHelper.Rebuild();
            string packageName = testHelper.ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith(".2.nupkg"));
        }
    }
}
