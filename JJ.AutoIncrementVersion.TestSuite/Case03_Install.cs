namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case03_Install
{
    /// <summary>
    /// Manual Test Plan → "Install"
    ///
    /// Steps:
    ///   1. Set Initial State.
    ///   2. Install JJ.AutoIncrementVersion package.
    ///   3. Rebuild – output shows .0.nupkg.
    ///   4. Auto-creates BuildNum.xml with expected content.
    ///   5. Auto-creates Directory.Build.props with expected content.
    /// </summary>
    [TestMethod]
    public void Case03_Install_AutoCreatesFilesAndBuildsWithZero()
    {
        using var testHelper = new TestHelper();

        testHelper.SetUninstalledState();

        var installResult = testHelper.InstallPackage();
        testHelper.LogResult($"Install exit code: {installResult.ExitCode}");

        testHelper.LogStep("Rebuild after install");
        CommandLineResult rebuildResult = testHelper.Rebuild();

        string? nupkg = testHelper.ExtractNupkgName(rebuildResult.Output);
        testHelper.LogResult($"Nupkg: {nupkg ?? "(none)"}");

        // TODO: Use AssertCore (global using static) from JJ.Framework.Testing.Core (from JJs-Dev-Package-Feed)

        AreEqual(0, rebuildResult.ExitCode, $"Build failed.\n{rebuildResult.Error}");
        IsTrue(
            testHelper.OutputContainsNupkgEndingWith(rebuildResult.Output, ".0.nupkg"),
            $"Expected nupkg ending with .0.nupkg but got: {nupkg}");

        IsTrue(testHelper.BuildNumXmlExists());
        string buildNumContent = testHelper.ReadBuildNumXml();
        IsTrue(buildNumContent.Contains("<BuildNum>"));
        IsTrue(buildNumContent.Contains("<DisableFastUpToDateCheck>True</DisableFastUpToDateCheck>"));
        IsTrue(buildNumContent.Contains("<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>"));

        IsTrue(testHelper.DirectoryBuildPropsExists());
        string dirPropsContent = testHelper.ReadDirectoryBuildProps();
        IsTrue(dirPropsContent.Contains("<BuildNum>0</BuildNum>"));
        IsTrue(dirPropsContent.Contains("Import Project=\"BuildNum.xml\""));

        testHelper.LogResult("PASS – Install auto-creates expected files and builds .0.nupkg");
    }
}
