namespace JJ.AutoIncrementVersion.Tests;

[TestClass]
public class Case09_Conditionals : TestBase
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
        LogTitle("Initialize");
        {
            InitInstalledState();
        }
        LogTitle("Set Condition");
        {
            EnsureDirPropsReleaseCondition();
            string content = ReadDirProps();
            IsTrue(content.Contains("$(Configuration)=='Release'", OrdinalIgnoreCase));
        }
        LogTitle("Release Builds Still Increment");
        {
            RebuildsIncrement();
        }
        LogTitle("But Debug Uses 0");
        {
            RebuildDebugUses0();
            LogLine();
            RebuildDebugUses0();
        }
        LogTitle("Release Continues Incrementing");
        {
            RebuildsIncrement();
        }
        LogTitle("Debug Deactivates BuildNum Again");
        {
            RebuildDebugUses0();
            LogLine();
            RebuildDebugUses0();
        }
    }

    private void RebuildDebugUses0()
    {
        int buildNum = GetBuildNumFromXml();
        IsTrue(buildNum != 0);
        string output = RebuildDebug();
        string packageName = ExtractPackageFileName(output);
        IsTrue(packageName.EndsWith(".0.nupkg"));
    }
}
