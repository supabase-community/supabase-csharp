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
        Assert.AreEqual(3, Widget.Add(1, 2));
    }
}
