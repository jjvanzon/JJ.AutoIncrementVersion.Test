
namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
[DoNotParallelize]
public class Case07_AutoRecreateFiles : TestHelper
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
        InitInstalledState();
        Rebuild();

        DeleteDirectoryBuildProps();
        IsFalse(DirPropsExists());

        RebuildExpectFail();


        IsTrue(DirPropsExists());

        // Subsequent builds succeed and increment
        int? previousBuildNum = null;
        for (int i = 0; i < 3; i++) // TODO: I like the retry loop and that it checks for increments.
        {
            string nextBuildOutput = Rebuild();

            int? nextBuildNum = ExtractPackageBuildNum(nextBuildOutput);

            // WOuld be nice if it checked for increments by exactly 1.

            if (previousBuildNum is not null && nextBuildNum is not null)
            {
                IsTrue(nextBuildNum > previousBuildNum);
            }

            previousBuildNum = nextBuildNum;
        }
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
        InitInstalledState();
        Rebuild();
        DeleteBuildNumXml();
        IsFalse(BuildNumXmlExists());
        Rebuild();
        IsTrue(BuildNumXmlExists());

        int newBuildNum = GetBuildNumFromXml();
        IsTrue(newBuildNum <= 1, $"After recreation, BuildNum should be 0 or 1 but was {newBuildNum}.");
    }

    /// <summary>
    /// Manual Test Plan → "Auto-Recreate Files" (both deleted)
    /// </summary>
    [TestMethod]
    public void Case07_DeleteBoth_ShowsSimilarEffect()
    {
        InitInstalledState();
        Rebuild();
        DeleteBuildNumXml();
        DeleteDirectoryBuildProps();

        try
        {
            Rebuild();
        }
        catch
        {
            // 1st build may fail
        }

        Rebuild();
        IsTrue(BuildNumXmlExists());
        IsTrue(DirPropsExists());
    }
}
