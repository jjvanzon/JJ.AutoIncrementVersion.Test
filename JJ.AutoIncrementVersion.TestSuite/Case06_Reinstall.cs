namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case06_Reinstall
{
    // TODO: One test has one step. This test has 2. Split. But also: all tests use same dependency, so can't run in parallel. Enforce that.
    /// <summary>
    /// Manual Test Plan → "Reinstall"
    ///
    /// Steps:
    ///   1. Uninstall, then reinstall the package.
    ///   2. Build should succeed, incrementing version each time.
    /// </summary>
    [TestMethod]
    public void Case06_Reinstall_BuildSucceedsAndIncrements()
    {
        using var testHelper = new TestHelper();

        // ── Establish working state, uninstall, then reinstall ──
        testHelper.LogStep("Start from committed state and build once");
        testHelper.SetInstalledState();
        testHelper.Rebuild(); // TODO: Swallowed error.

        testHelper.LogStep("Uninstall package");
        testHelper.UninstallPackage(); // TODO: Failure is ignored.

        testHelper.LogStep("Reinstall package");
        testHelper.InstallPackage();

        // ── Build and verify increment ──
        testHelper.LogStep("Build after reinstall – should succeed and increment");
        var build1 = testHelper.Rebuild();
        AreEqual(0, build1.ExitCode);

        int? firstNum = testHelper.ExtractBuildNumFromNupkgName(build1.Output);
        testHelper.LogResult($"First build after reinstall: BuildNum={firstNum}");

        var build2 = testHelper.Rebuild();
        AreEqual(0, build2.ExitCode);

        int? secondNum = testHelper.ExtractBuildNumFromNupkgName(build2.Output);
        testHelper.LogResult($"Second build after reinstall: BuildNum={secondNum}");

        if (firstNum is not null && secondNum is not null)
        {
            IsTrue(secondNum > firstNum);
        }

        testHelper.LogResult("PASS – Reinstall: build succeeds and version increments");
    }
}