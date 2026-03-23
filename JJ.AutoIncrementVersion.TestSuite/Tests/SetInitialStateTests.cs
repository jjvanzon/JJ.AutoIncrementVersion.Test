using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class SetInitialStateTests
{
    private TestHelper _h = null!;
   
    [TestInitialize]
    public void Init() => _h = new TestHelper(); // TODO: Init and CleanUp may as well be put in the test code iteself.

    [TestCleanup]
    public void Cleanup() => _h.Cleanup();

    [TestMethod]
    public void SetInitialState_ShouldNotCrash()
    {
        _h.SetInitialState();
    }
}
