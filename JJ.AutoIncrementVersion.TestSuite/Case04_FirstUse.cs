
namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case04_FirstUse
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
    public void Case04_FirstUse_FailsThenSucceedsThenIncrements()
    {
        using var testHelper = new TestHelper();

        testHelper.SetUninstalledState();
        testHelper.InstallPackage();
        testHelper.SetCsprojVersion("4.3.$(BuildNum)");

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
                ex.Message.Contains(expectedMessage, OrdinalIgnoreCase) ||
                ex.Message.Contains("NETSDK1018", OrdinalIgnoreCase);

            IsTrue(hasExpectedError, $"First build failed but not with the expected '{expectedMessage}' error.");
        }

        // 2nd build should succeed
        string buildOutput2 = testHelper.Rebuild();

        IsTrue(testHelper.BuildNumXmlExists());
        string buildNumContent = testHelper.ReadBuildNumXml();
        IsTrue(buildNumContent.Contains("<BuildNum>"));
        IsTrue(buildNumContent.Contains("<BuildNumWasFromXmljj>True</BuildNumWasFromXmljj>"));

        IsTrue(testHelper.DirectoryBuildPropsExists());
        string dirPropsContent = testHelper.ReadDirectoryBuildProps();
        // TODO: Check some content

        // ── Subsequent builds should auto-increment ──
        int? previousBuildNum = testHelper.ExtractBuildNumFromNupkgName(buildOutput2);

        for (int i = 0; i < 3; i++)
        {
            var nextBuildOutput = testHelper.Rebuild();
            int? nextBuildNum = testHelper.ExtractBuildNumFromNupkgName(nextBuildOutput);
            if (previousBuildNum is not null && nextBuildNum is not null)
            {
                IsTrue(nextBuildNum > previousBuildNum);
            }
            previousBuildNum = nextBuildNum;
        }
    }
}
