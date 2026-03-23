using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class UninstallReinstallTests
{
    private TestHelper _h = null!;

    [TestInitialize]
    public void Init() => _h = new TestHelper(); // TODO: Init and CleanUp may as well be put in the test code iteself.

    [TestCleanup]
    public void Cleanup() => _h.Cleanup();
    /// <summary>
    /// Manual Test Plan → "Uninstall"
    ///
    /// Steps:
    ///   1. Start from a working state (package installed, $(BuildNum) in version).
    ///   2. Uninstall package.
    ///   3. .xml and .Build.props should remain.
    ///   4. Build should succeed.
    ///   5. Version should stay frozen (no increment).
    /// </summary>
    [TestMethod]
    public void Uninstall_FilesRemainAndVersionFreezes()
    {
        // ── Establish working state ──
        _h.LogStep("Establish working state with package installed");
        //_h.GitRestoreAll(); // Do not do this. It's undoes our edits. Ensure state explicitly.
        // The committed state already has the package and $(BuildNum) in version.
        // Do a build to ensure BuildNum.xml/props exist.
        _h.Build();

        int buildNumBefore = _h.GetBuildNumFromXml();
        _h.LogResult($"BuildNum before uninstall: {buildNumBefore}");

        // ── Uninstall ──
        _h.LogStep("Uninstall package");
        _h.UninstallPackage(); // TODO: Failure is ignored, which is not good.
        _h.LogResult($"Package uninstalled. Has reference: {_h.CsprojHasPackageReference()}");

        // ── Verify files remain ──
        _h.LogStep("Verify BuildNum.xml and Directory.Build.props still exist");
        Assert.IsTrue(_h.BuildNumXmlExists(), "BuildNum.xml should remain after uninstall.");
        Assert.IsTrue(_h.DirectoryBuildPropsExists(), "Directory.Build.props should remain after uninstall.");
        _h.LogResult("Both files still present.");

        // ── Build should succeed ──
        _h.LogStep("Build after uninstall – should succeed");
        var buildResult = _h.Build();
        Assert.AreEqual(0, buildResult.ExitCode, $"Build failed after uninstall.\n{buildResult.Error}");
        _h.LogResult("Build succeeded.");

        // ── Version should be frozen ──
        _h.LogStep("Build again – version should stay frozen (no increment)");
        int buildNumAfterBuild = _h.GetBuildNumFromXml();
        var secondBuild = _h.Build();
        int buildNumAfterSecond = _h.GetBuildNumFromXml();
        _h.LogResult($"BuildNum after 1st build: {buildNumAfterBuild}, after 2nd build: {buildNumAfterSecond}");

        Assert.AreEqual(buildNumAfterBuild, buildNumAfterSecond,
            "BuildNum should not change when package is uninstalled.");

        _h.LogResult("PASS – Uninstall: files remain, build succeeds, version frozen");
    }

    // TODO: One test has one step. This test has 2. Split. But also: all tests use same dependency, so can't run in parallel. Enforce that.
    /// <summary>
    /// Manual Test Plan → "Reinstall"
    ///
    /// Steps:
    ///   1. Uninstall, then reinstall the package.
    ///   2. Build should succeed, incrementing version each time.
    /// </summary>
    [TestMethod]
    public void Reinstall_BuildSucceedsAndIncrements()
    {
        // ── Establish working state, uninstall, then reinstall ──
        _h.LogStep("Start from committed state and build once");
        _h.GitRestoreAll();
        _h.Build(); // TODO: Swallowed error.

        _h.LogStep("Uninstall package");
        _h.UninstallPackage(); // TODO: Failure is ignored.

        _h.LogStep("Reinstall package");
        _h.InstallPackage();

        // ── Build and verify increment ──
        _h.LogStep("Build after reinstall – should succeed and increment");
        var first = _h.Build();
        Assert.AreEqual(0, first.ExitCode, $"Build failed.\n{first.Error}");

        int? firstNum = _h.ExtractBuildNumFromNupkg(first.Output);
        _h.LogResult($"First build after reinstall: BuildNum={firstNum}");

        var second = _h.Build();
        Assert.AreEqual(0, second.ExitCode, $"Second build failed.\n{second.Error}");

        int? secondNum = _h.ExtractBuildNumFromNupkg(second.Output);
        _h.LogResult($"Second build after reinstall: BuildNum={secondNum}");

        if (firstNum is not null && secondNum is not null)
        {
            Assert.IsTrue(secondNum > firstNum,
                $"Expected increment: first={firstNum}, second={secondNum}");
        }

        _h.LogResult("PASS – Reinstall: build succeeds and version increments");
    }
}
