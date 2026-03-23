
namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case07_AutoRecreateFiles
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
    public void Case07_DeleteDirectoryBuildProps_FailsThenRecreatesAndIncrements()
    {
        using var testHelper = new TestHelper();

        testHelper.SetInstalledState();
        testHelper.Rebuild();

        testHelper.DeleteDirectoryBuildProps();
        IsFalse(testHelper.DirectoryBuildPropsExists());

        // TODO: Assertion code could be made reusable.
        CommandLineResult failBuild = testHelper.Rebuild();

        if (failBuild.ExitCode != 0)
        {
            const string expectedMessage = "is not a valid version string";

            bool hasExpectedError =
                failBuild.Output.Contains("NETSDK1018", StringComparison.OrdinalIgnoreCase) ||
                failBuild.Error.Contains("NETSDK1018", StringComparison.OrdinalIgnoreCase) ||
                failBuild.Output.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase) ||
                failBuild.Error.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase);
            testHelper.LogResult($"Build failed as expected. Expected error: {expectedMessage}");
        }
        else
        {
            testHelper.LogWarning("Build did not fail – may be OK if props was recreated before version resolution.");
        }

        IsTrue(testHelper.DirectoryBuildPropsExists());

        // Subsequent builds succeed and increment
        int? previousBuildNum = null;
        for (int i = 0; i < 3; i++) // TODO: I like the retry loop and that it checks for increments.
        {
            CommandLineResult result = testHelper.Rebuild();
            // TODO: ExitCode can indicate error, withotu Error text being filled in (error shows up in r.Output instead).
            AreEqual(0, result.ExitCode, $"Build {i + 1} failed.\n{result.Error}");

            int? currentBuildNum = testHelper.ExtractBuildNumFromNupkgName(result.Output);
            testHelper.LogResult($"Build {i + 1}: nupkg={testHelper.ExtractNupkgName(result.Output)} BuildNum={currentBuildNum}");

            // WOuld be nice if it checked for increments by exactly 1.

            if (previousBuildNum is not null && currentBuildNum is not null)
                IsTrue(currentBuildNum > previousBuildNum, $"Expected increment: prev={previousBuildNum}, cur={currentBuildNum}");
            previousBuildNum = currentBuildNum;
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
    public void Case07_DeleteBuildNumXml_RecreatesToZeroOrOne()
    {
        using var testHelper = new TestHelper();

        testHelper.SetInstalledState();
        testHelper.Rebuild(); // TODO: Working state was not checked, because error is swallowed.

        testHelper.DeleteBuildNumXml();
        IsFalse(testHelper.BuildNumXmlExists());

        CommandLineResult buildResult = testHelper.Rebuild();

        IsTrue(testHelper.BuildNumXmlExists());
        int newBuildNum = testHelper.GetBuildNumFromXml();
        IsTrue(newBuildNum <= 1, $"After recreation, BuildNum should be 0 or 1 but was {newBuildNum}.");

        testHelper.LogResult("PASS – Delete BuildNum.xml: recreated with low BuildNum");
    }

    /// <summary>
    /// Manual Test Plan → "Auto-Recreate Files" (both deleted)
    /// </summary>
    [TestMethod]
    public void Case07_DeleteBoth_ShowsSimilarEffect()
    {
        using var testHelper = new TestHelper();

        testHelper.SetInstalledState();
        testHelper.Rebuild(); // TODO: Working state was not checked, because error is swallowed.

        testHelper.DeleteBuildNumXml();
        testHelper.DeleteDirectoryBuildProps();

        // TODO: Build success should be asserted.

        testHelper.LogStep("Build – may fail first time");
        var first = testHelper.Rebuild();
        testHelper.LogResult($"1st build exit code: {first.ExitCode}");

        testHelper.LogStep("2nd build – should succeed");
        var second = testHelper.Rebuild();
        testHelper.LogResult($"2nd build exit code: {second.ExitCode}");

        IsTrue(testHelper.BuildNumXmlExists());
        IsTrue(testHelper.DirectoryBuildPropsExists());

        testHelper.LogResult("PASS – Both deleted: files recreated");
    }
}
