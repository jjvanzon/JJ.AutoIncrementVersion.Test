namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case02_RunWithoutPackage
{
    /// <summary>
    /// Manual Test Plan → "Set Initial State" + "Run Without Package"
    /// 
    /// Steps:
    ///   1. Uninstall existing JJ.AutoIncrementVersion package.
    ///   2. Delete BuildNum.xml and Directory.Build.props.
    ///   3. Replace $(BuildNum) with 0 in csproj.
    ///   4. Rebuild solution (= rebuild the project under test).
    ///   5. Output shows .0.nupkg
    /// </summary>
    [TestMethod]
    public void Case02_RunWithoutPackage_ProducesVersionEndingWithZero()
    {
        using var testHelper = new TestHelper();

        testHelper.SetUninstalledState();

        // TODO: Error for uninstall package is swallowed in the logic of SetInitialState, so we actually don't even know that we're running with or without the package.
        CommandLineResult buildResult = testHelper.Rebuild();

        string? nupkgFileName = testHelper.ExtractNupkgName(buildResult.Output);

        AreEqual(0, buildResult.ExitCode);
        IsNotNull(nupkgFileName);
        IsTrue(testHelper.OutputContainsNupkgEndingWith(buildResult.Output, ".0.nupkg"));

        testHelper.LogResult("PASS – Package version ends with .0.nupkg");
    }
}
