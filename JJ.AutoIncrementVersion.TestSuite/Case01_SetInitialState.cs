namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case01_SetInitialState
{
    [TestMethod]
    public void Case01_SetInitialState_ShouldNotCrash()
    {
        using var testHelper = new TestHelper();
        testHelper.InitUninstalled();
    }
}
