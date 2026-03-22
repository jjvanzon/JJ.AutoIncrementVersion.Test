using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class InstallTests
{
    private TestHelper _h = null!;

    [TestInitialize]
    public void Init() => _h = new TestHelper(TestContext);

    [TestCleanup]
    public void Cleanup() => _h.Cleanup();

    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Manual Test Plan → "Install"
    ///
    /// Steps:
    ///   1. Set Initial State.
    ///   2. Install JJ.AutoIncrementVersion package.
    ///   3. Rebuild – output shows .0.nupkg.
    ///   4. Auto-creates BuildNum.xml with expected content.
    ///   5. Auto-creates Directory.Build.props with expected content.
    /// </summary>
    [TestMethod]
    public void Install_AutoCreatesFilesAndBuildsWithZero()
    {
        // ── Set Initial State ──
        _h.SetInitialState();

        // ── Install package ──
        _h.LogStep("Install JJ.AutoIncrementVersion package");
        var installResult = _h.InstallPackage();
        _h.LogResult($"Install exit code: {installResult.ExitCode}");

        // ── Rebuild ──
        _h.LogStep("Rebuild after install");
        var buildResult = _h.Rebuild();

        string? nupkg = _h.ExtractNupkgName(buildResult.Output);
        _h.LogResult($"Nupkg: {nupkg ?? "(none)"}");

        Assert.AreEqual(0, buildResult.ExitCode, $"Build failed.\n{buildResult.Error}");
        Assert.IsTrue(
            _h.OutputContainsNupkgEndingWith(buildResult.Output, ".0.nupkg"),
            $"Expected nupkg ending with .0.nupkg but got: {nupkg}");

        // ── Verify auto-created BuildNum.xml ──
        _h.LogStep("Verify BuildNum.xml was auto-created");
        Assert.IsTrue(_h.BuildNumXmlExists(), "BuildNum.xml was not created.");
        string buildNumContent = _h.ReadBuildNumXml();
        _h.LogResult($"BuildNum.xml content: {buildNumContent.Trim()}");

        Assert.IsTrue(buildNumContent.Contains("<BuildNum>"),
            "BuildNum.xml does not contain <BuildNum> element.");
        Assert.IsTrue(buildNumContent.Contains("<DisableFastUpToDateCheck>True</DisableFastUpToDateCheck>"),
            "BuildNum.xml missing DisableFastUpToDateCheck.");
        Assert.IsTrue(buildNumContent.Contains("<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>"),
            "BuildNum.xml missing BuildNumWasFromXmljj.");

        // ── Verify auto-created Directory.Build.props ──
        _h.LogStep("Verify Directory.Build.props was auto-created");
        Assert.IsTrue(_h.DirectoryBuildPropsExists(), "Directory.Build.props was not created.");
        string propsContent = _h.ReadDirectoryBuildProps();
        _h.LogResult($"Directory.Build.props content: {propsContent.Trim()}");

        Assert.IsTrue(propsContent.Contains("<BuildNum>0</BuildNum>"),
            "Directory.Build.props should have <BuildNum>0</BuildNum>.");
        Assert.IsTrue(propsContent.Contains("Import Project=\"BuildNum.xml\""),
            "Directory.Build.props should import BuildNum.xml.");

        _h.LogResult("PASS – Install auto-creates expected files and builds .0.nupkg");
    }
}
