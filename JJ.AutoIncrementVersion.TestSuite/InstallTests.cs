namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class InstallTests
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
    public void Install_AutoCreatesFilesAndBuildsWithZero()
    {
        using var testHelper = new TestHelper();

        // ── Set Initial State ──
        testHelper.SetInitialState();

        // ── Install package ──
        testHelper.LogStep("Install JJ.AutoIncrementVersion package");
        var installResult = testHelper.InstallPackage();
        testHelper.LogResult($"Install exit code: {installResult.ExitCode}");

        // ── Rebuild ──
        testHelper.LogStep("Rebuild after install");
        var buildResult = testHelper.Rebuild();

        string? nupkg = testHelper.ExtractNupkgName(buildResult.Output);
        testHelper.LogResult($"Nupkg: {nupkg ?? "(none)"}");

        // TODO: Use AssertCore (global using static) from JJ.Framework.Testing.Core (from JJs-Dev-Package-Feed)

        Assert.AreEqual(0, buildResult.ExitCode, $"Build failed.\n{buildResult.Error}");
        Assert.IsTrue(
            testHelper.OutputContainsNupkgEndingWith(buildResult.Output, ".0.nupkg"),
            $"Expected nupkg ending with .0.nupkg but got: {nupkg}");

        // ── Verify auto-created BuildNum.xml ──
        testHelper.LogStep("Verify BuildNum.xml was auto-created");
        Assert.IsTrue(testHelper.BuildNumXmlExists(), "BuildNum.xml was not created.");
        string buildNumContent = testHelper.ReadBuildNumXml();
        testHelper.LogResult($"BuildNum.xml content: {buildNumContent.Trim()}");

        Assert.IsTrue(buildNumContent.Contains("<BuildNum>"),
            "BuildNum.xml does not contain <BuildNum> element.");
        Assert.IsTrue(buildNumContent.Contains("<DisableFastUpToDateCheck>True</DisableFastUpToDateCheck>"),
            "BuildNum.xml missing DisableFastUpToDateCheck.");
        Assert.IsTrue(buildNumContent.Contains("<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>"),
            "BuildNum.xml missing BuildNumWasFromXmljj.");

        // ── Verify auto-created Directory.Build.props ──
        testHelper.LogStep("Verify Directory.Build.props was auto-created");
        Assert.IsTrue(testHelper.DirectoryBuildPropsExists(), "Directory.Build.props was not created.");
        string propsContent = testHelper.ReadDirectoryBuildProps();
        testHelper.LogResult($"Directory.Build.props content: {propsContent.Trim()}");

        Assert.IsTrue(propsContent.Contains("<BuildNum>0</BuildNum>"),
            "Directory.Build.props should have <BuildNum>0</BuildNum>.");
        Assert.IsTrue(propsContent.Contains("Import Project=\"BuildNum.xml\""),
            "Directory.Build.props should import BuildNum.xml.");

        testHelper.LogResult("PASS – Install auto-creates expected files and builds .0.nupkg");
    }
}
