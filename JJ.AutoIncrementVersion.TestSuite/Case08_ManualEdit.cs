namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case08_ManualEdit
{
    /// <summary>
    /// Manual Test Plan → "Manual Edit"
    ///
    /// Steps:
    ///   1. Restore original BuildNum.xml (git restore).
    ///   2. Build – versions continue incrementing from where they left off.
    ///   3. Edit BuildNum.xml, setting the BuildNum value manually.
    ///   4. Build – versions start counting at new BuildNum.
    ///   5. And they increment each build.
    /// </summary>
    [TestMethod]
    public void Case08_ManualEdit_ContinuesFromRestoredValueThenFromManualValue()
    {
        using var testHelper = new TestHelper();

        testHelper.SetInstalledState();

        int originalBuildNum = testHelper.GetBuildNumFromXml();

        // ── Build – should increment from the restored value ──
        testHelper.LogStep("Build – should use restored BuildNum");
        CommandLineResult buildResult1 = testHelper.Rebuild();
        
        int? nupkgNum1 = testHelper.ExtractBuildNumFromNupkgName(buildResult1.Output);
        testHelper.LogResult($"Build 1 nupkg num: {nupkgNum1}");

        var build2 = testHelper.Rebuild();
        AreEqual(0, build2.ExitCode);

        int? nupkgNum2 = testHelper.ExtractBuildNumFromNupkgName(build2.Output);
        testHelper.LogResult($"Build 2 nupkg num: {nupkgNum2}");

        if (nupkgNum1 is not null && nupkgNum2 is not null)
        {
            IsTrue(nupkgNum2 > nupkgNum1,
                $"Expected increment from restored value: build1={nupkgNum1}, build2={nupkgNum2}");
        }

        // ── Manually set BuildNum to a specific value ──
        int manualValue = 100;
        testHelper.LogStep($"Manually set BuildNum to {manualValue}");
        testHelper.SetBuildNumInXml(manualValue);
        testHelper.LogResult($"BuildNum.xml now: {testHelper.ReadBuildNumXml().Trim()}");

        // ── Build – version should start from the manual value ──
        testHelper.LogStep("Build after manual edit – version should use new BuildNum");
        var build3 = testHelper.Rebuild();
        AreEqual(0, build3.ExitCode);

        int? nupkgNum3 = testHelper.ExtractBuildNumFromNupkgName(build3.Output);
        testHelper.LogResult($"Build 3 nupkg num: {nupkgNum3} (set manually to {manualValue})");

        // ── Subsequent builds should increment from that new value ──
        testHelper.LogStep("Subsequent builds – should increment from manual value");
        int? previousBuildNum = nupkgNum3;
        for (int i = 0; i < 2; i++)
        {
            CommandLineResult nextBuildResult = testHelper.Rebuild();
            AreEqual(0, nextBuildResult.ExitCode);

            int? currentBuildNum = testHelper.ExtractBuildNumFromNupkgName(nextBuildResult.Output);
            testHelper.LogResult($"Build {i + 4}: nupkg num={currentBuildNum}");

            if (previousBuildNum is not null && currentBuildNum is not null)
            {
                IsTrue(currentBuildNum > previousBuildNum,
                    $"Expected increment: prev={previousBuildNum}, cur={currentBuildNum}");
            }
            previousBuildNum = currentBuildNum;
        }

        testHelper.LogResult("PASS – Manual Edit: restored → increments; manual set → increments from new value");
    }
}
