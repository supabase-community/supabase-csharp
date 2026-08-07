using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Postgrest.Tests.Support;

namespace Postgrest.Tests.Paging;

/// <summary>
///     End-to-end proof that the paging operators window the result set the same way the equivalent LINQ
///     <c>Take</c>/<c>Skip</c> does: limit, offset, range (open and closed), and limit combined with offset.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class PagingQueryTests
{
    [TestMethod]
    public async Task Limit_ShouldReturnAtMostTheRequestedRows()
    {
        var client = LocalStack.Client();
        var limited = await client.Table<User>().Limit(2).Get();
        var all = await client.Table<User>().Get();
        limited.Models.Should().Equal(all.Models.Take(2).ToList());
    }

    [TestMethod]
    public async Task Offset_ShouldSkipTheLeadingRows()
    {
        var client = LocalStack.Client();
        var offset = await client.Table<User>().Offset(2).Get();
        var all = await client.Table<User>().Get();
        offset.Models.Should().Equal(all.Models.Skip(2).ToList());
    }

    [TestMethod]
    public async Task Range_ShouldSkipToTheStartBound()
    {
        var client = LocalStack.Client();
        var ranged = await client.Table<User>().Range(2).Get();
        var all = await client.Table<User>().Get();
        ranged.Models.Should().Equal(all.Models.Skip(2).ToList());
    }

    [TestMethod]
    public async Task Range_ShouldWindowBetweenTheStartAndEndBounds()
    {
        var client = LocalStack.Client();
        var ranged = await client.Table<User>().Range(1, 3).Get();
        var all = await client.Table<User>().Get();
        ranged.Models.Should().Equal(all.Models.Skip(1).Take(3).ToList());
    }

    [TestMethod]
    public async Task LimitAndOffset_ShouldWindowFromTheOffset()
    {
        var client = LocalStack.Client();
        var ranged = await client.Table<User>().Limit(1).Offset(3).Get();
        var all = await client.Table<User>().Get();
        ranged.Models.Should().Equal(all.Models.Skip(3).Take(1).ToList());
    }
}
