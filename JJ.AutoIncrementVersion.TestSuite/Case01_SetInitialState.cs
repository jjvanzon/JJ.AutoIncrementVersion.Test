namespace JJ.AutoIncrementVersion.TestSuite;

[TestClass]
public class Case01_SetInitialState
{
    [TestMethod]
    public void SetInitialState_ShouldNotCrash()
    {
        using var testHelper = new TestHelper();
        testHelper.SetUninstalledState();
    }
}
