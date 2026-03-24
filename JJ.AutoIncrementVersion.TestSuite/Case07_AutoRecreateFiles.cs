
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
        {
            InitInstalledState();
            IsTrue(BuildNumXmlExists());
            IsTrue(DirPropsExists());
            GetBuildNumFromXml();
            string outputInit = Rebuild();
            ExtractPackageFileName(outputInit);
            GetBuildNumFromXml();
            Log();
        }
        {
            DeleteDirectoryBuildProps();
            IsFalse(DirPropsExists());
            RebuildExpectFail();
            IsTrue(DirPropsExists());
            Log();
        }
        int buildNum1;
        {
            int buildNum = GetBuildNumFromXml();
            string output = Rebuild();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith($".{buildNum}.nupkg"));
            Log();
            buildNum1 = buildNum;
        }
        int buildNum2;
        {
            int buildNum = GetBuildNumFromXml();
            string output = Rebuild();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith($".{buildNum}.nupkg"));
            Log();
            buildNum2 = buildNum;
        }
        IsTrue(buildNum2 == buildNum1 + 1);

        int buildNum3;
        {
            int buildNum = GetBuildNumFromXml();
            string output = Rebuild();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith($".{buildNum}.nupkg"));
            Log();

            buildNum3 = buildNum;
        }
        IsTrue(buildNum3 == buildNum2 + 1);

        //return;

        int prev = default;
        
        // Subsequent builds succeed and increment

        // TODO: Put in helper and reuse.
        int repeats = 3;
        for (int i = 0; i < repeats; i++)
        {
            int buildNum = GetBuildNumFromXml();
            string output = Rebuild();
            string packageName = ExtractPackageFileName(output);
            IsTrue(packageName.EndsWith($".{buildNum}.nupkg"));

            if (i != 0)
            {
                IsTrue(buildNum == prev + 1);
            }

            prev = buildNum;

            Log();
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
