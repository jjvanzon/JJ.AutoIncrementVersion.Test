namespace JJ.AutoIncrementVersion.Tests;

[TestClass]
public class Case04_FirstUse : TestBase
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
        LogTitle("Initialize");
        {
            InitUninstalled();
        }

        LogTitle("Install");
        {
            InstallPackage();
        }

        LogTitle("First Use");
        {
            SetProjPatchNum("$(BuildNum)");
            RebuildExpectFail();
            IsFalse(BuildNumXmlExists());
            IsTrue(DirPropsExists()); // There anyway
        }

        LogTitle("2nd Build Succeeds");
        {
            string output = Rebuild();
            IsTrue(BuildNumXmlExists());
            IsTrue(DirPropsExists());
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith(".0.nupkg"));
        }

        LogTitle("Next Builds Increment");
        {
            RebuildsIncrement(from: 1);
        }
    }
}
