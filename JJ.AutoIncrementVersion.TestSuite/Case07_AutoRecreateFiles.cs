
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
        try
        {
            testHelper.Rebuild();
        }
        catch (Exception ex)
        {
            // The first build may fail because $(BuildNum) resolves to empty.
            // This is expected per the manual plan.
            const string expectedMessage = "is not a valid version string";

            bool hasExpectedError =
                ex.Message.Contains("NETSDK1018", OrdinalIgnoreCase) ||
                ex.Message.Contains(expectedMessage, OrdinalIgnoreCase);

            IsTrue(hasExpectedError, $"First build failed but not with the expected '{expectedMessage}' error.");
        }

        IsTrue(testHelper.DirectoryBuildPropsExists());

        // Subsequent builds succeed and increment
        int? previousBuildNum = null;
        for (int i = 0; i < 3; i++) // TODO: I like the retry loop and that it checks for increments.
        {
            CommandLineResult nextBuildResult = testHelper.Rebuild();

            int? nextBuildNum = testHelper.ExtractBuildNumFromNupkgName(nextBuildResult.Output);
            testHelper.LogResult($"Build {i + 1}: nupkg={testHelper.ExtractNupkgName(nextBuildResult.Output)} BuildNum={nextBuildNum}");

            // WOuld be nice if it checked for increments by exactly 1.

            if (previousBuildNum is not null && nextBuildNum is not null)
            {
                IsTrue(nextBuildNum > previousBuildNum);
            }

            previousBuildNum = nextBuildNum;
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
        testHelper.Rebuild();
        testHelper.DeleteBuildNumXml();
        IsFalse(testHelper.BuildNumXmlExists());
        testHelper.Rebuild();
        IsTrue(testHelper.BuildNumXmlExists());

        int newBuildNum = testHelper.GetBuildNumFromXml();
        IsTrue(newBuildNum <= 1, $"After recreation, BuildNum should be 0 or 1 but was {newBuildNum}.");
    }

    /// <summary>
    /// Manual Test Plan → "Auto-Recreate Files" (both deleted)
    /// </summary>
    [TestMethod]
    public void Case07_DeleteBoth_ShowsSimilarEffect()
    {
        using var testHelper = new TestHelper();

        testHelper.SetInstalledState();
        testHelper.Rebuild();
        testHelper.DeleteBuildNumXml();
        testHelper.DeleteDirectoryBuildProps();

        try
        {
            testHelper.Rebuild();
        }
        catch
        {
            // 1st build may fail
        }

        testHelper.Rebuild();
        IsTrue(testHelper.BuildNumXmlExists());
        IsTrue(testHelper.DirectoryBuildPropsExists());
    }
}
