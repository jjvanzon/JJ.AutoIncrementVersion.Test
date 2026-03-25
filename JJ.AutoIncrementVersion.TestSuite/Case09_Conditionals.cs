namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case09_Conditionals : TestHelper
{
    /// <summary>
    /// Manual Test Plan → "Conditionals"
    ///
    /// Tests conditional BuildNum.xml inclusion from Directory.Build.props.
    ///
    /// Steps:
    ///   1. Ensure Directory.Build.props has the Release condition.
    ///   2. Build for Release – BuildNum increments.
    ///   3. Build for Debug – uses BuildNum 0.
    ///   4. Swap a few times to see if Release continues with original range.
    /// </summary>
    [TestMethod]
    public void Case09_Conditionals_ReleaseIncrementsDebugUsesZero()
    {
        {
            InitInstalledState();
            Log();
        }
        {
            EnsureDirPropsReleaseCondition();
            string content = ReadDirProps();
            IsTrue(content.Contains("$(Configuration)=='Release'", OrdinalIgnoreCase));
            Log();
        }
        {
            Log("Release builds increment:");
            Log();
            RebuildsIncrement();
        }
        {
            Log("Debug builds use 0:");
            Log();
        }
        {
            int buildNum = GetBuildNumFromXml();
            IsTrue(buildNum != 0);
            string output = RebuildDebug();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith(".0.nupkg"));
            Log();
        }
        {
            int buildNum = GetBuildNumFromXml();
            IsTrue(buildNum != 0);
            string output = RebuildDebug();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith(".0.nupkg"));
            Log();
        }
        {
            Log("Release continues incrementing:");
            Log();
            RebuildsIncrement();
        }
        {
            Log("Debug deactivates BuildNum again:");
            Log();
        }
        {
            int buildNum = GetBuildNumFromXml();
            IsTrue(buildNum != 0);
            string output = RebuildDebug();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith(".0.nupkg"));
            Log();
        }
        {
            int buildNum = GetBuildNumFromXml();
            IsTrue(buildNum != 0);
            string output = RebuildDebug();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith(".0.nupkg"));
            Log();
        }
    }
}
