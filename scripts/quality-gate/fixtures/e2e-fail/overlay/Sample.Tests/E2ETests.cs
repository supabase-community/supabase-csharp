using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample;

namespace Sample.Tests;

[TestClass]
public class E2ETests
{
    [TestMethod]
    [TestCategory("E2E")]
    public void Roundtrip_ShouldSucceed()
    {
        // Deliberate acceptance failure: 1 + 1 is 2, not 3. With the stack reachable
        // the full gate runs this, and because E2E is blocking its failure must FAIL
        // the gate — a red acceptance test blocks the merge like any red unit test.
        Assert.AreEqual(3, Widget.Add(1, 1));
    }
}
