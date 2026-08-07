#region

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using static Gotrue.Tests.TestUtils;
using static Supabase.Gotrue.Constants;

#endregion

namespace Gotrue.Tests.Stateless;

/// <summary>
///     End-to-end service-role administration through the stateless client against the live stack: inviting,
///     creating, listing, reading, updating and deleting users with an explicit service-role key per call.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class StatelessAdminTests : StatelessFixture
{
    [TestMethod]
    public async Task InviteUserByEmail_ShouldSucceed() => (await this.Client.InviteUserByEmail(RandomEmail(), ServiceRoleKey, Options)).Should().BeTrue();

    [TestMethod]
    public async Task DeleteUser_ShouldRemoveTheUser()
    {
        var session = (await this.Client.SignUp(RandomEmail(), Password, Options))!;
        (await this.Client.DeleteUser(session.User!.Id!, ServiceRoleKey, Options)).Should().BeTrue();
    }

    [TestMethod]
    public async Task ListUsers_ShouldReturnRegisteredUsers() => (await this.Client.ListUsers(ServiceRoleKey, Options))!.Users.Should().NotBeEmpty();

    [TestMethod]
    public async Task ListUsers_ShouldReturnDistinctPages_GivenPagination()
    {
        var page1 = (await this.Client.ListUsers(ServiceRoleKey, Options, page: 1, perPage: 1))!;
        var page2 = (await this.Client.ListUsers(ServiceRoleKey, Options, page: 2, perPage: 1))!;
        page1.Users.Should().ContainSingle();
        page2.Users.Should().ContainSingle();
        page1.Users[0].Id.Should().NotBe(page2.Users[0].Id);
    }

    [TestMethod]
    public async Task ListUsers_ShouldOrderResults_GivenSortOrder()
    {
        var descending = (await this.Client.ListUsers(ServiceRoleKey, Options, sortBy: "created_at", sortOrder: SortOrder.Descending))!;
        var ascending = (await this.Client.ListUsers(ServiceRoleKey, Options, sortBy: "created_at", sortOrder: SortOrder.Ascending))!;
        descending.Users[0].Id.Should().NotBe(ascending.Users[0].Id);
    }

    [TestMethod]
    public async Task ListUsers_ShouldMatchOnlyTheFilteredDomain_GivenEmailFilter()
    {
        var unmatched = (await this.Client.ListUsers(ServiceRoleKey, Options, "@example.com"))!;
        var matched = (await this.Client.ListUsers(ServiceRoleKey, Options, "@supabase.io"))!;
        matched.Users.Should().NotBeEmpty();
        unmatched.Users.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetUserById_ShouldReturnMatchingUser()
    {
        var listed = (await this.Client.ListUsers(ServiceRoleKey, Options, page: 1, perPage: 1))!.Users[0];
        var byId = (await this.Client.GetUserById(ServiceRoleKey, Options, listed.Id!))!;
        byId.Id.Should().Be(listed.Id);
        byId.Email.Should().Be(listed.Email);
    }

    [TestMethod]
    public async Task CreateUser_ShouldCreateUsersWithAndWithoutAttributes()
    {
        (await this.Client.CreateUser(ServiceRoleKey, Options, RandomEmail(), Password)).Should().NotBeNull();
        var withMetadata = (await this.Client.CreateUser(ServiceRoleKey, Options, RandomEmail(), Password, new AdminUserAttributes
        {
            UserMetadata = new Dictionary<string, object> { { "firstName", "123" } },
        }))!;
        withMetadata.UserMetadata["firstName"].Should().Be("123");
        (await this.Client.CreateUser(ServiceRoleKey, Options, new AdminUserAttributes { Email = RandomEmail(), Password = Password }))
            .Should().NotBeNull();
    }

    [TestMethod]
    public async Task UpdateUserById_ShouldChangeEmail()
    {
        var created = (await this.Client.CreateUser(ServiceRoleKey, Options, RandomEmail(), Password))!;
        var updated = (await this.Client.UpdateUserById(ServiceRoleKey, Options, created.Id!, new AdminUserAttributes { Email = RandomEmail() }))!;
        updated.Id.Should().Be(created.Id);
        updated.Email.Should().NotBe(created.Email);
    }
}
