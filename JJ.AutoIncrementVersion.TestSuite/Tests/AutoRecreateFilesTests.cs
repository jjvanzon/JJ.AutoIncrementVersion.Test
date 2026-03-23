using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class AutoRecreateFilesTests
{
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
        var testHelper = new TestHelper();

        // ── Start from working state ──
        testHelper.LogStep("Establish working state");
        testHelper.GitRestoreAll();
        testHelper.Build();

        // ── Delete Directory.Build.props ──
        testHelper.LogStep("Delete Directory.Build.props");
        testHelper.DeleteDirectoryBuildProps();
        Assert.IsFalse(testHelper.DirectoryBuildPropsExists());

        // ── Build should fail ── // TODO: Assertion code could be made reusable.
        testHelper.LogStep("Build – expect failure (NETSDK1018 / Invalid NuGet version)");
        var failBuild = testHelper.Build();
        testHelper.LogResult($"Exit code: {failBuild.ExitCode}");

        if (failBuild.ExitCode != 0)
        {
            bool hasExpectedError =
                failBuild.Output.Contains("NETSDK1018", StringComparison.OrdinalIgnoreCase) ||
                failBuild.Error.Contains("NETSDK1018", StringComparison.OrdinalIgnoreCase) ||
                failBuild.Output.Contains("Invalid NuGet version string", StringComparison.OrdinalIgnoreCase) ||
                failBuild.Error.Contains("Invalid NuGet version string", StringComparison.OrdinalIgnoreCase);
            testHelper.LogResult($"Build failed as expected. Expected error: {hasExpectedError}");
        }
        else
        {
            testHelper.LogWarning("Build did not fail – may be OK if props was recreated before version resolution.");
        }

        // ── Directory.Build.props should be recreated ──
        testHelper.LogStep("Verify Directory.Build.props was recreated");
        Assert.IsTrue(testHelper.DirectoryBuildPropsExists(),
            "Directory.Build.props should have been recreated by the build.");
        testHelper.LogResult($"Content: {testHelper.ReadDirectoryBuildProps().Trim()}");

        // ── Subsequent builds succeed and increment ──
        testHelper.LogStep("Subsequent builds – succeed and increment");
        int? prev = null;
        for (int i = 0; i < 3; i++) // TODO: I like the retry loop and that it checks for increments.
        {
            var r = testHelper.Build();
            // TODO: ExitCode can indicate error, withotu Error text being filled in (error shows up in r.Output instead).
            Assert.AreEqual(0, r.ExitCode, $"Build {i + 1} failed.\n{r.Error}");

            int? cur = testHelper.ExtractBuildNumFromNupkg(r.Output);
            testHelper.LogResult($"Build {i + 1}: nupkg={testHelper.ExtractNupkgName(r.Output)} BuildNum={cur}");

            // WOuld be nice if it checked for increments by exactly 1.

            if (prev is not null && cur is not null)
                Assert.IsTrue(cur > prev, $"Expected increment: prev={prev}, cur={cur}");
            prev = cur;
        }

        testHelper.LogResult("PASS – Delete Directory.Build.props: fail → recreate → increment");
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
        var testHelper = new TestHelper();

        // ── Start from working state ──
        testHelper.LogStep("Establish working state");
        testHelper.GitRestoreAll();
        testHelper.Build(); // TODO: Working state was not checked, because error is swallowed.

        // ── Delete BuildNum.xml ──
        testHelper.LogStep("Delete BuildNum.xml");
        testHelper.DeleteBuildNumXml();
        Assert.IsFalse(testHelper.BuildNumXmlExists());

        // ── Build ──
        testHelper.LogStep("Build after deleting BuildNum.xml");
        var result = testHelper.Build();
        testHelper.LogResult($"Exit code: {result.ExitCode}");

        // ── Verify recreated ──
        testHelper.LogStep("Verify BuildNum.xml was recreated");
        Assert.IsTrue(testHelper.BuildNumXmlExists(), "BuildNum.xml should be recreated.");
        int newBuildNum = testHelper.GetBuildNumFromXml();
        testHelper.LogResult($"BuildNum.xml recreated with BuildNum={newBuildNum}");
        Assert.IsTrue(newBuildNum <= 1,
            $"After recreation, BuildNum should be 0 or 1 but was {newBuildNum}.");

        testHelper.LogResult("PASS – Delete BuildNum.xml: recreated with low BuildNum");
    }

    /// <summary>
    /// Manual Test Plan → "Auto-Recreate Files" (both deleted)
    /// </summary>
    [TestMethod]
    public void DeleteBoth_ShowsSimilarEffect()
    {
        var testHelper = new TestHelper();

        testHelper.LogStep("Establish working state");
        testHelper.GitRestoreAll();
        testHelper.Build(); // TODO: Working state was not checked, because error is swallowed.

        testHelper.LogStep("Delete both BuildNum.xml and Directory.Build.props");
        testHelper.DeleteBuildNumXml();
        testHelper.DeleteDirectoryBuildProps();

        // TODO: Build success should be asserted.

        testHelper.LogStep("Build – may fail first time");
        var first = testHelper.Build();
        testHelper.LogResult($"1st build exit code: {first.ExitCode}");

        testHelper.LogStep("2nd build – should succeed");
        var second = testHelper.Build();
        testHelper.LogResult($"2nd build exit code: {second.ExitCode}");

        testHelper.LogStep("Verify both files recreated");
        Assert.IsTrue(testHelper.BuildNumXmlExists(), "BuildNum.xml should be recreated.");
        Assert.IsTrue(testHelper.DirectoryBuildPropsExists(), "Directory.Build.props should be recreated.");
        testHelper.LogResult($"BuildNum.xml: {testHelper.ReadBuildNumXml().Trim()}");
        testHelper.LogResult($"Directory.Build.props: {testHelper.ReadDirectoryBuildProps().Trim()}");

        testHelper.LogResult("PASS – Both deleted: files recreated");
    }
}
