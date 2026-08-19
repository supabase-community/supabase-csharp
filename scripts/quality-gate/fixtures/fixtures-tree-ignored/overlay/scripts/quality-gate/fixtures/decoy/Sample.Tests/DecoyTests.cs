using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample;

namespace Sample.Tests;

[TestClass]
public class DecoyTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Add_ShouldReturnSum()
    {
        Assert.AreEqual(3, Decoy.Add(1, 2));
    }
}
