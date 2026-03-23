using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class ManualEditTests
{
    private TestHelper _h = null!;

    [TestInitialize]
    public void Init() => _h = new TestHelper(); // TODO: Init and CleanUp may as well be put in the test code iteself.

    [TestCleanup]
    public void Cleanup() => _h.Cleanup();

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
        // ── Restore original state ──
        _h.LogStep("Restore original BuildNum.xml via git");
        _h.GitRestoreAll(); // Doing a git restore of our work mid-test automatically is a big no no.

        int originalBuildNum = _h.GetBuildNumFromXml();
        _h.LogResult($"Original BuildNum from XML: {originalBuildNum}");

        // ── Build – should increment from the restored value ──
        _h.LogStep("Build – should use restored BuildNum");
        var build1 = _h.Build();
        // TODO: Assertion not clear (build1.Error doesn't contain error. It's embedded in build1.Output).
        Assert.AreEqual(0, build1.ExitCode, $"Build failed.\n{build1.Error}");
        
        int? nupkgNum1 = _h.ExtractBuildNumFromNupkg(build1.Output);
        _h.LogResult($"Build 1 nupkg num: {nupkgNum1}");

        var build2 = _h.Build();
        Assert.AreEqual(0, build2.ExitCode, $"Build 2 failed.\n{build2.Error}");

        int? nupkgNum2 = _h.ExtractBuildNumFromNupkg(build2.Output);
        _h.LogResult($"Build 2 nupkg num: {nupkgNum2}");

        if (nupkgNum1 is not null && nupkgNum2 is not null)
        {
            Assert.IsTrue(nupkgNum2 > nupkgNum1,
                $"Expected increment from restored value: build1={nupkgNum1}, build2={nupkgNum2}");
        }

        // ── Manually set BuildNum to a specific value ──
        int manualValue = 100;
        _h.LogStep($"Manually set BuildNum to {manualValue}");
        _h.SetBuildNumInXml(manualValue);
        _h.LogResult($"BuildNum.xml now: {_h.ReadBuildNumXml().Trim()}");

        // ── Build – version should start from the manual value ──
        _h.LogStep("Build after manual edit – version should use new BuildNum");
        var build3 = _h.Build();
        Assert.AreEqual(0, build3.ExitCode, $"Build 3 failed.\n{build3.Error}");

        int? nupkgNum3 = _h.ExtractBuildNumFromNupkg(build3.Output);
        _h.LogResult($"Build 3 nupkg num: {nupkgNum3} (set manually to {manualValue})");

        // ── Subsequent builds should increment from that new value ──
        _h.LogStep("Subsequent builds – should increment from manual value");
        int? prev = nupkgNum3;
        for (int i = 0; i < 2; i++)
        {
            var next = _h.Build();
            Assert.AreEqual(0, next.ExitCode, $"Build {i + 4} failed.\n{next.Error}");

            int? cur = _h.ExtractBuildNumFromNupkg(next.Output);
            _h.LogResult($"Build {i + 4}: nupkg num={cur}");

            if (prev is not null && cur is not null)
            {
                Assert.IsTrue(cur > prev,
                    $"Expected increment: prev={prev}, cur={cur}");
            }
            prev = cur;
        }

        _h.LogResult("PASS – Manual Edit: restored → increments; manual set → increments from new value");
    }
}
