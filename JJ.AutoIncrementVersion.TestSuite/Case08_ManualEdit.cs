namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case08_ManualEdit : TestBase
{
    /// <summary>
    /// Manual Test Plan → "Manual Edit"
    ///
    /// Steps:
    ///   1. Restore original BuildNum.xml (git restore).
    ///   2. Build – versions continue incrementing from where they left off.
    ///   3. Edit BuildNum.xml, setting the BuildNum value manually.
    ///   4. Build – versions start counting at new BuildNum.
    ///   5. And they increment each build.
    /// </summary>
    [TestMethod]
    public void Case08_ManualEdit_ContinuesFromRestoredValueThenFromManualValue()
    {
        LogTitle("Verify Initial State");
        {
            InitInstalledState();
            Log();
            RebuildsIncrement(repeats: 2);
            Log();
            var xml = ReadBuildNumXml();
            Log("BuildNum.xml = " + xml.TrimEnd());
        }

        LogTitle("Edit BuildNum.xml");
        {
            SetBuildNumInXml(100);
            var xml = ReadBuildNumXml();
            Log("BuildNum.xml = " + xml);
        }

        LogTitle("Continues from Manual BuildNum");
        {
            RebuildsIncrement(from: 100);
            Log();
        }
    }
}
