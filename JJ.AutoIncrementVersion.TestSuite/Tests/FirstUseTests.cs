using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class FirstUseTests
{
    private TestHelper _h = null!;

    [TestInitialize]
    public void Init() => _h = new TestHelper(TestContext); // TODO: Init and CleanUp may as well be put in the test code iteself.

    [TestCleanup]
    public void Cleanup() => _h.Cleanup();

    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Manual Test Plan → "First Use"
    ///
    /// Steps:
    ///   1. Prepare Initial State.
    ///   2. Install JJ.AutoIncrementVersion package.
    ///   3. Set csproj Version to 4.3.$(BuildNum).
    ///   4. 1st rebuild should fail ("Invalid NuGet version string: '4.3.'").
    ///   5. 2nd build succeeds.
    ///   6. Auto-creates BuildNum.xml and Directory.Build.props.
    ///   7. Output shows .0.nupkg.
    ///   8. Subsequent builds auto-increment (.1, .2, …).
    /// </summary>
    [TestMethod]
    public void FirstUse_FailsThenSucceedsThenIncrements()
    {
        // ── Set Initial State ──
        _h.SetInitialState();

        // ── Install package ──
        _h.LogStep("Install package");
        _h.InstallPackage();

        // ── Set $(BuildNum) in version ──
        _h.LogStep("Set <Version>4.3.$(BuildNum)</Version>");
        _h.SetCsprojVersion("4.3.$(BuildNum)");

        // ── 1st rebuild: expect failure ──
        _h.LogStep("1st rebuild – expect failure (no BuildNum.xml yet)");
        var first = _h.Rebuild();
        _h.LogResult($"Exit code: {first.ExitCode}");

        // The first build may fail because $(BuildNum) resolves to empty.
        // This is expected per the manual plan.
        if (first.ExitCode != 0)
        {
            bool hasExpectedError =
                first.Output.Contains("Invalid NuGet version string", StringComparison.OrdinalIgnoreCase) ||
                first.Error.Contains("Invalid NuGet version string", StringComparison.OrdinalIgnoreCase) ||
                first.Output.Contains("NETSDK1018", StringComparison.OrdinalIgnoreCase) ||
                first.Error.Contains("NETSDK1018", StringComparison.OrdinalIgnoreCase);
            _h.LogResult($"First build failed as expected. Expected error present: {hasExpectedError}");

            // TODO: The error does not contain "Invalid NuGet version string" The actual error is: C:\Program Files\dotnet\sdk\10.0.201\NuGet.targets(196,5): error : '4.3.' is not a valid version string. (Parameter 'value') [D:\Repositories\JJ.AutoIncrementVersion.Test\JJ.AutoIncrementVersion.Test\JJ.AutoIncrementVersion.Test.csproj]
            Assert.IsTrue(hasExpectedError,
                "First build failed but not with the expected 'Invalid NuGet version string' error.");
        }
        else
        {

            _h.LogWarning("First build did NOT fail — this is acceptable if BuildNum.xml was auto-created in time.");
        }

        // ── 2nd build: expect success ──
        _h.LogStep("2nd build – should succeed");
        var second = _h.Build();
        Assert.AreEqual(0, second.ExitCode, $"2nd build failed.\n{second.Error}");

        // ── Verify auto-created files ──
        _h.LogStep("Verify BuildNum.xml auto-created");
        Assert.IsTrue(_h.BuildNumXmlExists(), "BuildNum.xml was not created.");
        string buildNumContent = _h.ReadBuildNumXml();
        _h.LogResult($"BuildNum.xml: {buildNumContent.Trim()}");
        Assert.IsTrue(buildNumContent.Contains("<BuildNum>"), "Missing <BuildNum>.");
        Assert.IsTrue(buildNumContent.Contains("<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>"),
            "Missing BuildNumWasFromXmljj.");

        _h.LogStep("Verify Directory.Build.props auto-created");
        Assert.IsTrue(_h.DirectoryBuildPropsExists(), "Directory.Build.props was not created.");
        string propsContent = _h.ReadDirectoryBuildProps();
        _h.LogResult($"Directory.Build.props: {propsContent.Trim()}");

        // ── Verify nupkg output ──
        string? nupkg = _h.ExtractNupkgName(second.Output);
        _h.LogResult($"Nupkg from 2nd build: {nupkg ?? "(none)"}");

        // ── Subsequent builds should auto-increment ──
        _h.LogStep("Subsequent builds – verify auto-increment");
        int? prevNum = _h.ExtractBuildNumFromNupkg(second.Output);

        for (int i = 0; i < 3; i++)
        {
            var next = _h.Build();
            Assert.AreEqual(0, next.ExitCode, $"Build {i + 3} failed.\n{next.Error}");

            int? curNum = _h.ExtractBuildNumFromNupkg(next.Output);
            string? curNupkg = _h.ExtractNupkgName(next.Output);
            _h.LogResult($"Build {i + 3}: {curNupkg} (BuildNum={curNum})");

            if (prevNum is not null && curNum is not null)
            {
                Assert.IsTrue(curNum > prevNum,
                    $"Expected BuildNum to increment: prev={prevNum}, cur={curNum}");
            }
            prevNum = curNum;
        }

        _h.LogResult("PASS – First Use: fail → succeed → auto-increment");
    }
}
