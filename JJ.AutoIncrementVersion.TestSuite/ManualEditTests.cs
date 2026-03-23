using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class ManualEditTests
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
    public void ManualEdit_ContinuesFromRestoredValueThenFromManualValue()
    {
        using var testHelper = new TestHelper();

        // ── Restore original state ──
        testHelper.LogStep("Restore original BuildNum.xml from embedded resources");
        testHelper.RestoreAll();

        int originalBuildNum = testHelper.GetBuildNumFromXml();
        testHelper.LogResult($"Original BuildNum from XML: {originalBuildNum}");

        // ── Build – should increment from the restored value ──
        testHelper.LogStep("Build – should use restored BuildNum");
        var build1 = testHelper.Build();
        // TODO: Assertion not clear (build1.Error doesn't contain error. It's embedded in build1.Output).
        Assert.AreEqual(0, build1.ExitCode, $"Build failed.\n{build1.Error}");
        
        int? nupkgNum1 = testHelper.ExtractBuildNumFromNupkg(build1.Output);
        testHelper.LogResult($"Build 1 nupkg num: {nupkgNum1}");

        var build2 = testHelper.Build();
        Assert.AreEqual(0, build2.ExitCode, $"Build 2 failed.\n{build2.Error}");

        int? nupkgNum2 = testHelper.ExtractBuildNumFromNupkg(build2.Output);
        testHelper.LogResult($"Build 2 nupkg num: {nupkgNum2}");

        if (nupkgNum1 is not null && nupkgNum2 is not null)
        {
            Assert.IsTrue(nupkgNum2 > nupkgNum1,
                $"Expected increment from restored value: build1={nupkgNum1}, build2={nupkgNum2}");
        }

        // ── Manually set BuildNum to a specific value ──
        int manualValue = 100;
        testHelper.LogStep($"Manually set BuildNum to {manualValue}");
        testHelper.SetBuildNumInXml(manualValue);
        testHelper.LogResult($"BuildNum.xml now: {testHelper.ReadBuildNumXml().Trim()}");

        // ── Build – version should start from the manual value ──
        testHelper.LogStep("Build after manual edit – version should use new BuildNum");
        var build3 = testHelper.Build();
        Assert.AreEqual(0, build3.ExitCode, $"Build 3 failed.\n{build3.Error}");

        int? nupkgNum3 = testHelper.ExtractBuildNumFromNupkg(build3.Output);
        testHelper.LogResult($"Build 3 nupkg num: {nupkgNum3} (set manually to {manualValue})");

        // ── Subsequent builds should increment from that new value ──
        testHelper.LogStep("Subsequent builds – should increment from manual value");
        int? prev = nupkgNum3;
        for (int i = 0; i < 2; i++)
        {
            var next = testHelper.Build();
            Assert.AreEqual(0, next.ExitCode, $"Build {i + 4} failed.\n{next.Error}");

            int? cur = testHelper.ExtractBuildNumFromNupkg(next.Output);
            testHelper.LogResult($"Build {i + 4}: nupkg num={cur}");

            if (prev is not null && cur is not null)
            {
                Assert.IsTrue(cur > prev,
                    $"Expected increment: prev={prev}, cur={cur}");
            }
            prev = cur;
        }

        testHelper.LogResult("PASS – Manual Edit: restored → increments; manual set → increments from new value");
    }
}
