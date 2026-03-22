using JJ.AutoIncrementVersion.TestSuite.Helpers;

namespace JJ.AutoIncrementVersion.TestSuite.Tests;

[TestClass]
public class SetInitialStateTests
{
    private TestHelper _h = null!;
   
    [TestInitialize]
    public void Init() => _h = new TestHelper(TestContext); // TODO: Init and CleanUp may as well be put in the test code iteself.

    [TestCleanup]
    public void Cleanup() => _h.Cleanup();

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void SetInitialState_ShouldNotCrash()
    {
        _h.SetInitialState();
    }
}
