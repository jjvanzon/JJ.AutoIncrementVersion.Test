using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class RunWithoutPackageTests
{
    private TestHelper _h = null!;

    [TestInitialize]
    public void Init() => _h = new TestHelper(TestContext);

    [TestCleanup]
    public void Cleanup() => _h.Cleanup();

    public TestContext TestContext { get; set; } = null!;

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
        // ── Set Initial State ──
        _h.SetInitialState();

        // ── Run Without Package ──
        _h.LogStep("Run Without Package – Rebuild");
        var result = _h.Rebuild();

        _h.LogStep("Verify output contains .0.nupkg");
        string? nupkg = _h.ExtractNupkgName(result.Output);
        _h.LogResult($"Extracted nupkg name: {nupkg ?? "(none)"}");

        Assert.AreEqual(0, result.ExitCode, $"Build failed.\n{result.Error}");
        Assert.IsNotNull(nupkg, "No nupkg file name found in build output.");
        Assert.IsTrue(
            _h.OutputContainsNupkgEndingWith(result.Output, ".0.nupkg"),
            $"Expected nupkg ending with .0.nupkg but got: {nupkg}");

        _h.LogResult("PASS – Package version ends with .0.nupkg");
    }
}
