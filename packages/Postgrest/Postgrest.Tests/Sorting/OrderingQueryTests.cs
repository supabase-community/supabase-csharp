using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Postgrest.Tests.Support;
using static Supabase.Postgrest.Constants;

namespace Postgrest.Tests.Sorting;

/// <summary>
///     End-to-end proof that PostgREST returns rows in the order the SDK requests, matching an in-memory
///     LINQ sort — single- and multi-column.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class OrderingQueryTests
{
    [TestMethod]
    public async Task Order_ShouldSortByASingleColumn()
    {
        var client = LocalStack.Client();
        var ordered = await client.Table<User>().Order("username", Ordering.Descending).Get();
        var unordered = await client.Table<User>().Get();
        ordered.Models.Should().Equal(unordered.Models.OrderByDescending(u => u.Username).ToList());
    }

    [TestMethod]
    public async Task Order_ShouldSortByMultipleColumns()
    {
        var client = LocalStack.Client();
        var ordered = await client.Table<User>()
            .Order(u => u.Username!, Ordering.Descending)
            .Order(u => u.Status!, Ordering.Descending)
            .Get();
        var unordered = await client.Table<User>().Get();
        ordered.Models.Should().Equal(unordered.Models
            .OrderByDescending(u => u.Username).ThenByDescending(u => u.Status).ToList());
    }
}
