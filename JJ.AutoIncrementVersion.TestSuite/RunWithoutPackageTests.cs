namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class RunWithoutPackageTests
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
    public void RunWithoutPackage_ProducesVersionEndingWithZero()
    {
        using var testHelper = new TestHelper();

        // ── Set Initial State ──
        testHelper.SetInitialState();

        // TODO: Error for uninstall package is swallowed in the logic of SetInitialState, so we actually don't even know that we're running with or without the package.

        // ── Run Without Package ──
        testHelper.LogStep("Run Without Package – Rebuild");
        var result = testHelper.Rebuild();

        testHelper.LogStep("Verify output contains .0.nupkg");
        string? nupkg = testHelper.ExtractNupkgName(result.Output);
        testHelper.LogResult($"Extracted nupkg name: {nupkg ?? "(none)"}");

        Assert.AreEqual(0, result.ExitCode, $"Build failed.\n{result.Error}");
        Assert.IsNotNull(nupkg, "No nupkg file name found in build output.");
        Assert.IsTrue(
            testHelper.OutputContainsNupkgEndingWith(result.Output, ".0.nupkg"),
            $"Expected nupkg ending with .0.nupkg but got: {nupkg}");

        testHelper.LogResult("PASS – Package version ends with .0.nupkg");
    }
}
