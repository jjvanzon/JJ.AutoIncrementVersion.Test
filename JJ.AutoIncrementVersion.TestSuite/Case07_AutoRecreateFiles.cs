namespace JJ.AutoIncrementVersion.TestSuite;

//[TestClass]
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
        {
            InitInstalledState();
            IsTrue(DirPropsExists());
            GetBuildNumFromXml();
            string output = Rebuild();
            ExtractPackageFileName(output);
            GetBuildNumFromXml();
            Log();
        }
        {
            DeleteDirProps();
            IsFalse(DirPropsExists());
            RebuildExpectFail();
            IsTrue(DirPropsExists());
            Log();
        }
        
        RebuildsIncrement();
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
    public void Case07_DeleteBuildNumXml_RecreatesToZeroOrOne()
    {
        {
            InitInstalledState();
            IsTrue(BuildNumXmlExists());
            GetBuildNumFromXml();
            string output = Rebuild();
            ExtractPackageFileName(output);
            Log();
        }
        {
            DeleteBuildNumXml();
            IsFalse(BuildNumXmlExists());
            string output = Rebuild();
            ExtractPackageFileName(output);
            IsTrue(BuildNumXmlExists());
        }
        {
            int newBuildNum = GetBuildNumFromXml();
            IsTrue(newBuildNum <= 1);
            Log();
        }

        RebuildsIncrement();
    }

    /// <summary>
    /// Manual Test Plan → "Auto-Recreate Files" (both deleted)
    /// </summary>
    [TestMethod]
    public void Case07_DeleteBoth_ShowsSimilarEffect()
    {
        {
            InitInstalledState();
            IsTrue(BuildNumXmlExists());
            GetBuildNumFromXml();
            string output = Rebuild();
            ExtractPackageFileName(output);
            Log();
        }
        {
            DeleteBuildNumXml();
            IsFalse(BuildNumXmlExists());
            DeleteDirProps();
            IsFalse(DirPropsExists());
            Log();
        }
        {
            RebuildExpectFail();
            IsTrue(DirPropsExists());
            IsFalse(BuildNumXmlExists()); // Does not auto-create that soon.
            Log();
        }
        {
            string output = Rebuild();
            IsTrue(DirPropsExists());
            IsTrue(BuildNumXmlExists());
            ExtractPackageFileName(output);
            Log();
        }
        {
            int newBuildNum = GetBuildNumFromXml();
            IsTrue(newBuildNum <= 1);
            Log("BuildNum starts low");
            Log();
        }
    
        RebuildsIncrement();
    }
}
