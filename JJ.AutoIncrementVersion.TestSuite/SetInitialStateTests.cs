namespace JJ.AutoIncrementVersion.TestSuite;

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
