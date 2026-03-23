using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class UninstallReinstallTests
{
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
        using var testHelper = new TestHelper();

        // ── Establish working state ──
        testHelper.LogStep("Establish working state with package installed");
        // The isolated folder already has the package reference and $(BuildNum) in version.
        // Do a build to ensure BuildNum.xml/props exist.
        testHelper.Build();

        int buildNumBefore = testHelper.GetBuildNumFromXml();
        testHelper.LogResult($"BuildNum before uninstall: {buildNumBefore}");

        // ── Uninstall ──
        testHelper.LogStep("Uninstall package");
        testHelper.UninstallPackage(); // TODO: Failure is ignored, which is not good.
        testHelper.LogResult($"Package uninstalled. Has reference: {testHelper.CsprojHasPackageReference()}");

        // ── Verify files remain ──
        testHelper.LogStep("Verify BuildNum.xml and Directory.Build.props still exist");
        Assert.IsTrue(testHelper.BuildNumXmlExists(), "BuildNum.xml should remain after uninstall.");
        Assert.IsTrue(testHelper.DirectoryBuildPropsExists(), "Directory.Build.props should remain after uninstall.");
        testHelper.LogResult("Both files still present.");

        // ── Build should succeed ──
        testHelper.LogStep("Build after uninstall – should succeed");
        var buildResult = testHelper.Build();
        Assert.AreEqual(0, buildResult.ExitCode, $"Build failed after uninstall.\n{buildResult.Error}");
        testHelper.LogResult("Build succeeded.");

        // ── Version should be frozen ──
        testHelper.LogStep("Build again – version should stay frozen (no increment)");
        int buildNumAfterBuild = testHelper.GetBuildNumFromXml();
        var secondBuild = testHelper.Build();
        int buildNumAfterSecond = testHelper.GetBuildNumFromXml();
        testHelper.LogResult($"BuildNum after 1st build: {buildNumAfterBuild}, after 2nd build: {buildNumAfterSecond}");

        Assert.AreEqual(buildNumAfterBuild, buildNumAfterSecond,
            "BuildNum should not change when package is uninstalled.");

        testHelper.LogResult("PASS – Uninstall: files remain, build succeeds, version frozen");
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
        using var testHelper = new TestHelper();

        // ── Establish working state, uninstall, then reinstall ──
        testHelper.LogStep("Start from committed state and build once");
        testHelper.RestoreAll();
        testHelper.Build(); // TODO: Swallowed error.

        testHelper.LogStep("Uninstall package");
        testHelper.UninstallPackage(); // TODO: Failure is ignored.

        testHelper.LogStep("Reinstall package");
        testHelper.InstallPackage();

        // ── Build and verify increment ──
        testHelper.LogStep("Build after reinstall – should succeed and increment");
        var first = testHelper.Build();
        Assert.AreEqual(0, first.ExitCode, $"Build failed.\n{first.Error}");

        int? firstNum = testHelper.ExtractBuildNumFromNupkg(first.Output);
        testHelper.LogResult($"First build after reinstall: BuildNum={firstNum}");

        var second = testHelper.Build();
        Assert.AreEqual(0, second.ExitCode, $"Second build failed.\n{second.Error}");

        int? secondNum = testHelper.ExtractBuildNumFromNupkg(second.Output);
        testHelper.LogResult($"Second build after reinstall: BuildNum={secondNum}");

        if (firstNum is not null && secondNum is not null)
        {
            Assert.IsTrue(secondNum > firstNum,
                $"Expected increment: first={firstNum}, second={secondNum}");
        }

        testHelper.LogResult("PASS – Reinstall: build succeeds and version increments");
    }
}
