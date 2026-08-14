using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample;

namespace Sample.Tests;

[TestClass]
public class WidgetTests
{
    // Only an E2E-categorised test exists, so TestCategory!=E2E selects nothing.
    [TestMethod]
    [TestCategory("E2E")]
    public void Add_ShouldReturnSum()
    {
        Assert.AreEqual(3, Widget.Add(1, 2));
    }
}
