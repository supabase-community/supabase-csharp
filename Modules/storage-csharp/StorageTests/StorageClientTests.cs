using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace StorageTests;

[TestClass]
public class StorageClientTests
{
    [TestMethod("Can use gettable headers")]
    public void CanUseGettableHeaders()
    {
        var initialHeaders = new Dictionary<string, string> { { "Testing", "1234" } };

        var client = new Supabase.Storage.Client("http://localhost:5000", initialHeaders)
        {
            GetHeaders = () => new Dictionary<string, string>
            {
                ["Dynamic"] = "4567",
                ["Testing"] = "4567"
            }
        };

        Assert.AreEqual("1234", client.Headers["Testing"]);
        Assert.AreEqual("4567", client.Headers["Dynamic"]);
    }
}