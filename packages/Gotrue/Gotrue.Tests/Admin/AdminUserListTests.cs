#region

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Gotrue.Tests.TestUtils;
using static Supabase.Gotrue.Constants;

#endregion

namespace Gotrue.Tests.Admin;

/// <summary>
///     End-to-end service-role listing of users against the live stack: total listing, page/per-page
///     pagination, sort order, and email filtering.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class AdminUserListTests : AdminFixture
{
    [TestMethod]
    public async Task ListUsers_ShouldReturnRegisteredUsers() => (await this.Admin.ListUsers())!.Users.Should().NotBeEmpty();

    [TestMethod]
    public async Task ListUsers_ShouldReturnDistinctPages_GivenPagination()
    {
        var page1 = await this.Admin.ListUsers(page: 1, perPage: 1);
        var page2 = await this.Admin.ListUsers(page: 2, perPage: 1);
        page1!.Users.Should().ContainSingle();
        page2!.Users.Should().ContainSingle();
        page1.Users[0].Id.Should().NotBe(page2.Users[0].Id);
    }

    [TestMethod]
    public async Task ListUsers_ShouldOrderResults_GivenSortOrder()
    {
        var ascending = await this.Admin.ListUsers(sortBy: "created_at", sortOrder: SortOrder.Ascending);
        var descending = await this.Admin.ListUsers(sortBy: "created_at", sortOrder: SortOrder.Descending);
        ascending!.Users[0].Id.Should().NotBe(descending!.Users[0].Id);
    }

    [TestMethod]
    public async Task ListUsers_ShouldMatchOnlyTheFilteredDomain_GivenEmailFilter()
    {
        (await this.Admin.CreateUser(RandomEmail(), Password)).Should().NotBeNull();
        var unmatched = await this.Admin.ListUsers("@nonexistingrandomemailprovider.com");
        var matched = await this.Admin.ListUsers("@supabase.io");
        matched!.Users.Should().NotBeEmpty();
        unmatched!.Users.Should().BeEmpty();
    }
}
