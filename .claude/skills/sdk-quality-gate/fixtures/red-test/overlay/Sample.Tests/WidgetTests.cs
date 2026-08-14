using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample;

namespace Sample.Tests;

[TestClass]
public class WidgetTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Add_ShouldReturnSum()
    {
        // Wrong expectation on purpose: 1 + 2 is 3, not 4.
        Assert.AreEqual(4, Widget.Add(1, 2));
    }
}
