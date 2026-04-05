namespace JJ.AutoIncrementVersion.Tests;

[TestClass]
public class Case01_SetInitialState : TestBase
{
    [TestMethod]
    public void Case01_SetInitialState_ShouldNotCrash()
    {
        LogTitle("Initial State");
        Log("Should not crash");
        InitUninstalled();
        Log("Did no crash");
    }
}
