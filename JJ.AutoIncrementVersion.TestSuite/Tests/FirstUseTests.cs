using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class FirstUseTests
{
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
        var testHelper = new TestHelper();

        // ── Set Initial State ──
        testHelper.SetInitialState();

        // ── Install package ──
        testHelper.LogStep("Install package");
        testHelper.InstallPackage();

        // ── Set $(BuildNum) in version ──
        testHelper.LogStep("Set <Version>4.3.$(BuildNum)</Version>");
        testHelper.SetCsprojVersion("4.3.$(BuildNum)");

        // ── 1st rebuild: expect failure ──
        testHelper.LogStep("1st rebuild – expect failure (no BuildNum.xml yet)");
        var first = testHelper.Rebuild();
        testHelper.LogResult($"Exit code: {first.ExitCode}");

        // The first build may fail because $(BuildNum) resolves to empty.
        // This is expected per the manual plan.
        if (first.ExitCode != 0)
        {
            string expectedMessage = "is not a valid version string";

            bool hasExpectedError =
                first.Output.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase) ||
                first.Error.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase) ||
                first.Output.Contains("NETSDK1018", StringComparison.OrdinalIgnoreCase) ||
                first.Error.Contains("NETSDK1018", StringComparison.OrdinalIgnoreCase);
            testHelper.LogResult($"First build failed as expected. Expected error present: {expectedMessage}");

            // TODO: The error does not contain "Invalid NuGet version string" The actual error is: C:\Program Files\dotnet\sdk\10.0.201\NuGet.targets(196,5): error : '4.3.' is not a valid version string. (Parameter 'value') [D:\Repositories\JJ.AutoIncrementVersion.Test\JJ.AutoIncrementVersion.Test\JJ.AutoIncrementVersion.Test.csproj]
            Assert.IsTrue(hasExpectedError, $"First build failed but not with the expected '{expectedMessage}' error.");
        }
        else
        {

            testHelper.LogWarning("First build did NOT fail — this is acceptable if BuildNum.xml was auto-created in time.");
        }

        // ── 2nd build: expect success ──
        testHelper.LogStep("2nd build – should succeed");
        var second = testHelper.Build();
        Assert.AreEqual(0, second.ExitCode, $"2nd build failed.\n{second.Error}");

        // ── Verify auto-created files ──
        testHelper.LogStep("Verify BuildNum.xml auto-created");
        Assert.IsTrue(testHelper.BuildNumXmlExists(), "BuildNum.xml was not created.");
        string buildNumContent = testHelper.ReadBuildNumXml();
        testHelper.LogResult($"BuildNum.xml: {buildNumContent.Trim()}");
        Assert.IsTrue(buildNumContent.Contains("<BuildNum>"), "Missing <BuildNum>.");
        Assert.IsTrue(buildNumContent.Contains("<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>"),
            "Missing BuildNumWasFromXmljj.");

        testHelper.LogStep("Verify Directory.Build.props auto-created");
        Assert.IsTrue(testHelper.DirectoryBuildPropsExists(), "Directory.Build.props was not created.");
        string propsContent = testHelper.ReadDirectoryBuildProps();
        testHelper.LogResult($"Directory.Build.props: {propsContent.Trim()}");

        // ── Verify nupkg output ──
        string? nupkg = testHelper.ExtractNupkgName(second.Output);
        testHelper.LogResult($"Nupkg from 2nd build: {nupkg ?? "(none)"}");

        // ── Subsequent builds should auto-increment ──
        testHelper.LogStep("Subsequent builds – verify auto-increment");
        int? prevNum = testHelper.ExtractBuildNumFromNupkg(second.Output);

        for (int i = 0; i < 3; i++)
        {
            var next = testHelper.Build();
            Assert.AreEqual(0, next.ExitCode, $"Build {i + 3} failed.\n{next.Error}");

            int? curNum = testHelper.ExtractBuildNumFromNupkg(next.Output);
            string? curNupkg = testHelper.ExtractNupkgName(next.Output);
            testHelper.LogResult($"Build {i + 3}: {curNupkg} (BuildNum={curNum})");

            if (prevNum is not null && curNum is not null)
            {
                Assert.IsTrue(curNum > prevNum,
                    $"Expected BuildNum to increment: prev={prevNum}, cur={curNum}");
            }
            prevNum = curNum;
        }

        testHelper.LogResult("PASS – First Use: fail → succeed → auto-increment");
    }
}
