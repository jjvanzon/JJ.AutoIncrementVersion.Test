namespace JJ.AutoIncrementVersion.Tests;

[TestClass]
[DoNotParallelize]
public class Case07_AutoRecreateFiles : TestBase
{
    /// <summary>
    /// Manual Test Plan → "Auto-Recreate Files" (Directory.Build.props deleted)
    ///
    /// Steps:
    /// 1) Delete Directory.Build.props.
    /// 2) Build should fail with NETSDK1018.
    /// 3) But recreated Directory.Build.props.
    /// 4) Subsequent builds succeed, incrementing version.
    /// </summary>
    [TestMethod]
    public void Case07_DeleteDirectoryBuildProps_FailsThenRecreatesAndIncrements()
    {
        LogTitle("Verify Working State");
        {
            InitInstalledState();
            IsTrue(DirPropsExists());
            LogLine();
            RebuildsIncrement(repeats: 2);
        }

        LogTitle("Delete Dir.Props");
        {
            IsTrue(DirPropsExists());
            DeleteDirProps();
            IsFalse(DirPropsExists());
        }

        LogTitle("Auto-Create Dir.Props");
        {
            RebuildExpectFail();
            IsTrue(DirPropsExists());
        }

        LogTitle("Continues Operation");
        {
            RebuildsIncrement();
        }
    }

    /// <summary>
    /// Manual Test Plan → "Auto-Recreate Files" (BuildNum.xml deleted)
    ///
    /// Steps:
    /// 1) Delete BuildNum.xml.
    /// 2) Build.
    /// 3) BuildNum.xml should be recreated.
    /// 4) Versions start at BuildNum 0 or 1 again.
    /// </summary>
    [TestMethod]
    public void Case07_DeleteBuildNumXml_RecreatesResetsToZeroOrOne()
    {
        LogTitle("Verify Working State");
        {
            InitInstalledState();
            IsTrue(BuildNumXmlExists());
            LogLine();
            RebuildsIncrement(repeats: 2);
        }

        LogTitle("Delete XML");
        {
            IsTrue(BuildNumXmlExists());
            DeleteBuildNumXml();
            IsFalse(BuildNumXmlExists());
        }
        
        LogTitle("Auto-Create XML");
        {
            string output = Rebuild();
            IsTrue(BuildNumXmlExists());
            ExtractPackageFileName(output);
        }

        LogTitle("Continues Operation");
        {
            RebuildsIncrement(from: 1);
        }
    }

    /// <summary>
    /// Manual Test Plan → "Auto-Recreate Files" (both deleted)
    /// </summary>
    [TestMethod]
    public void Case07_DeleteBoth_ShowsSimilarEffect()
    {
        LogTitle("Verify Working State");
        {
            InitInstalledState();
            IsTrue(BuildNumXmlExists());
            IsTrue(DirPropsExists());
            LogLine();
            RebuildsIncrement();
            /*
            GetBuildNumFromXml();
            string output = Rebuild();
            ExtractPackageFileName(output);
            */
        }

        LogTitle("Delete Files");
        {
            DeleteBuildNumXml();
            IsFalse(BuildNumXmlExists());
            DeleteDirProps();
            IsFalse(DirPropsExists());
        }

        LogTitle("Auto-Create Dir.Props");
        {
            RebuildExpectFail();
            IsTrue(DirPropsExists());
            IsFalse(BuildNumXmlExists()); // Does not auto-create immediately.
        }

        LogTitle("Auto-Create XML");
        {
            string output = Rebuild();
            IsTrue(DirPropsExists());
            IsTrue(BuildNumXmlExists());
            ExtractPackageFileName(output);
        }

        LogTitle("Continues Operation");
        {
            RebuildsIncrement(from: 1);
        }
    }
}
