using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class SetInitialStateTests
{
    [TestMethod]
    public void SetInitialState_ShouldNotCrash()
    {
        using var testHelper = new TestHelper();
        testHelper.SetInitialState();
    }
}
