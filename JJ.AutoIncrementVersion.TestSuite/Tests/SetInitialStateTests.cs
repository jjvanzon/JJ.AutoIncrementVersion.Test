using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class SetInitialStateTests
{
    [TestMethod]
    public void SetInitialState_ShouldNotCrash()
    {
        var testHelper = new TestHelper();
        testHelper.SetInitialState();
    }
}
