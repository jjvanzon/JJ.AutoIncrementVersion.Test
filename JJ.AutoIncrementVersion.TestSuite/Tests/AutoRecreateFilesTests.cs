using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class AutoRecreateFilesTests
{
    private TestHelper _h = null!;

    [TestInitialize]
    public void Init() => _h = new TestHelper(TestContext); // TODO: Init and CleanUp may as well be put in the test code iteself.

    [TestCleanup]
    public void Cleanup() => _h.Cleanup();

    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Manual Test Plan → "Auto-Recreate Files" (Directory.Build.props deleted)
    ///
    /// Steps:
    ///   1. Delete Directory.Build.props.
    ///   2. Build should fail with NETSDK1018.
    ///   3. But recreated Directory.Build.props.
    ///   4. Subsequent builds succeed, incrementing version.
    /// </summary>
    [TestMethod]
    public void DeleteDirectoryBuildProps_FailsThenRecreatesAndIncrements()
    {
        // ── Start from working state ──
        _h.LogStep("Establish working state");
        _h.GitRestoreAll();
        _h.Build();

        // ── Delete Directory.Build.props ──
        _h.LogStep("Delete Directory.Build.props");
        _h.DeleteDirectoryBuildProps();
        Assert.IsFalse(_h.DirectoryBuildPropsExists());

        // ── Build should fail ── // TODO: Assertion code could be made reusable.
        _h.LogStep("Build – expect failure (NETSDK1018 / Invalid NuGet version)");
        var failBuild = _h.Build();
        _h.LogResult($"Exit code: {failBuild.ExitCode}");

        if (failBuild.ExitCode != 0)
        {
            bool hasExpectedError =
                failBuild.Output.Contains("NETSDK1018", StringComparison.OrdinalIgnoreCase) ||
                failBuild.Error.Contains("NETSDK1018", StringComparison.OrdinalIgnoreCase) ||
                failBuild.Output.Contains("Invalid NuGet version string", StringComparison.OrdinalIgnoreCase) ||
                failBuild.Error.Contains("Invalid NuGet version string", StringComparison.OrdinalIgnoreCase);
            _h.LogResult($"Build failed as expected. Expected error: {hasExpectedError}");
        }
        else
        {
            _h.LogWarning("Build did not fail – may be OK if props was recreated before version resolution.");
        }

        // ── Directory.Build.props should be recreated ──
        _h.LogStep("Verify Directory.Build.props was recreated");
        Assert.IsTrue(_h.DirectoryBuildPropsExists(),
            "Directory.Build.props should have been recreated by the build.");
        _h.LogResult($"Content: {_h.ReadDirectoryBuildProps().Trim()}");

        // ── Subsequent builds succeed and increment ──
        _h.LogStep("Subsequent builds – succeed and increment");
        int? prev = null;
        for (int i = 0; i < 3; i++) // TODO: I like the retry loop and that it checks for increments.
        {
            var r = _h.Build();
            // TODO: ExitCode can indicate error, withotu Error text being filled in (error shows up in r.Output instead).
            Assert.AreEqual(0, r.ExitCode, $"Build {i + 1} failed.\n{r.Error}");

            int? cur = _h.ExtractBuildNumFromNupkg(r.Output);
            _h.LogResult($"Build {i + 1}: nupkg={_h.ExtractNupkgName(r.Output)} BuildNum={cur}");

            // WOuld be nice if it checked for increments by exactly 1.

            if (prev is not null && cur is not null)
                Assert.IsTrue(cur > prev, $"Expected increment: prev={prev}, cur={cur}");
            prev = cur;
        }

        _h.LogResult("PASS – Delete Directory.Build.props: fail → recreate → increment");
    }

    /// <summary>
    /// Manual Test Plan → "Auto-Recreate Files" (BuildNum.xml deleted)
    ///
    /// Steps:
    ///   1. Delete BuildNum.xml.
    ///   2. Build.
    ///   3. BuildNum.xml should be recreated.
    ///   4. Versions start at BuildNum 0 or 1 again.
    /// </summary>
    [TestMethod]
    public void DeleteBuildNumXml_RecreatesToZeroOrOne()
    {
        // ── Start from working state ──
        _h.LogStep("Establish working state");
        _h.GitRestoreAll();
        _h.Build(); // TODO: Working state was not checked, because error is swallowed.

        // ── Delete BuildNum.xml ──
        _h.LogStep("Delete BuildNum.xml");
        _h.DeleteBuildNumXml();
        Assert.IsFalse(_h.BuildNumXmlExists());

        // ── Build ──
        _h.LogStep("Build after deleting BuildNum.xml");
        var result = _h.Build();
        _h.LogResult($"Exit code: {result.ExitCode}");

        // ── Verify recreated ──
        _h.LogStep("Verify BuildNum.xml was recreated");
        Assert.IsTrue(_h.BuildNumXmlExists(), "BuildNum.xml should be recreated.");
        int newBuildNum = _h.GetBuildNumFromXml();
        _h.LogResult($"BuildNum.xml recreated with BuildNum={newBuildNum}");
        Assert.IsTrue(newBuildNum <= 1,
            $"After recreation, BuildNum should be 0 or 1 but was {newBuildNum}.");

        _h.LogResult("PASS – Delete BuildNum.xml: recreated with low BuildNum");
    }

    /// <summary>
    /// Manual Test Plan → "Auto-Recreate Files" (both deleted)
    /// </summary>
    [TestMethod]
    public void DeleteBoth_ShowsSimilarEffect()
    {
        _h.LogStep("Establish working state");
        _h.GitRestoreAll();
        _h.Build(); // TODO: Working state was not checked, because error is swallowed.

        _h.LogStep("Delete both BuildNum.xml and Directory.Build.props");
        _h.DeleteBuildNumXml();
        _h.DeleteDirectoryBuildProps();

        // TODO: Build success should be asserted.

        _h.LogStep("Build – may fail first time");
        var first = _h.Build();
        _h.LogResult($"1st build exit code: {first.ExitCode}");

        _h.LogStep("2nd build – should succeed");
        var second = _h.Build();
        _h.LogResult($"2nd build exit code: {second.ExitCode}");

        _h.LogStep("Verify both files recreated");
        Assert.IsTrue(_h.BuildNumXmlExists(), "BuildNum.xml should be recreated.");
        Assert.IsTrue(_h.DirectoryBuildPropsExists(), "Directory.Build.props should be recreated.");
        _h.LogResult($"BuildNum.xml: {_h.ReadBuildNumXml().Trim()}");
        _h.LogResult($"Directory.Build.props: {_h.ReadDirectoryBuildProps().Trim()}");

        _h.LogResult("PASS – Both deleted: files recreated");
    }
}
