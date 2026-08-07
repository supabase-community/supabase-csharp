#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using static Gotrue.Tests.TestUtils;

#endregion

namespace Gotrue.Tests.Admin;

/// <summary>
///     End-to-end service-role user administration against the live stack: creating, reading, updating,
///     banning, inviting and deleting users.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class AdminUserManagementTests : AdminFixture
{
    [TestMethod]
    public async Task CreateUser_ShouldCreateUsersWithAndWithoutAttributes()
    {
        (await this.Admin.CreateUser(RandomEmail(), Password)).Should().NotBeNull();
        var withMetadata = await this.Admin.CreateUser(RandomEmail(), Password, new AdminUserAttributes
        {
            UserMetadata = new Dictionary<string, object> { { "firstName", "123" } },
            AppMetadata = new Dictionary<string, object> { { "roles", new List<string> { "editor", "publisher" } } },
        });
        withMetadata!.UserMetadata["firstName"].Should().Be("123");
        (await this.Admin.CreateUser(new AdminUserAttributes { Email = RandomEmail(), Password = Password })).Should().NotBeNull();
    }

    [TestMethod]
    public async Task UpdateUserById_ShouldPersistUserMetadata()
    {
        var created = await this.Admin.CreateUser(RandomEmail(), Password);
        var updated = await this.Admin.UpdateUserById(created!.Id!, new AdminUserAttributes
        {
            UserMetadata = new Dictionary<string, object> { { "hello", "updated" } },
        });
        updated!.UserMetadata["hello"].Should().Be("updated");
        var reloaded = await this.Admin.GetUserById(created.Id!);
        reloaded!.UserMetadata["hello"].Should().Be("updated");
    }

    [TestMethod]
    public async Task UpdateUserById_ShouldChangeEmail()
    {
        var created = await this.Admin.CreateUser(RandomEmail(), Password);
        var updated = await this.Admin.UpdateUserById(created!.Id ?? throw new InvalidOperationException(),
            new AdminUserAttributes { Email = RandomEmail() });
        updated!.Id.Should().Be(created.Id);
        updated.Email.Should().NotBe(created.Email);
    }

    [TestMethod]
    public async Task UpdateUserById_ShouldBanThenUnbanUser()
    {
        var created = await this.Admin.CreateUser(RandomEmail(), Password);
        var banSeconds = RandomNumber();
        var bannedUntil = DateTime.UtcNow + TimeSpan.FromSeconds(banSeconds);
        var banned = await this.Admin.UpdateUserById(created!.Id ?? throw new InvalidOperationException(),
            new AdminUserAttributes { BanDuration = $"{banSeconds}s" });
        using (new AssertionScope())
        {
            banned!.Id.Should().Be(created.Id);
            banned.BannedUntil.Should().NotBeNull();
            (banned.BannedUntil!.Value - bannedUntil).Duration().TotalSeconds.Should().BeLessThan(1);
        }
        var unbanned = await this.Admin.UpdateUserById(created.Id!, new AdminUserAttributes { BanDuration = "none" });
        unbanned!.BannedUntil.Should().BeNull();
    }

    [TestMethod]
    public async Task GetUserById_ShouldReturnMatchingUser()
    {
        var listed = (await this.Admin.ListUsers(page: 1, perPage: 1))!.Users[0];
        var byId = await this.Admin.GetUserById(listed.Id ?? throw new InvalidOperationException());
        byId!.Id.Should().Be(listed.Id);
        byId.Email.Should().Be(listed.Email);
    }

    [TestMethod]
    public async Task InviteUserByEmail_ShouldSucceed() => (await this.Admin.InviteUserByEmail(RandomEmail())).Should().BeTrue();

    [TestMethod]
    public async Task DeleteUser_ShouldRemoveTheUser()
    {
        var created = await this.Admin.CreateUser(RandomEmail(), Password);
        (await this.Admin.DeleteUser(created!.Id ?? throw new InvalidOperationException())).Should().BeTrue();
    }
}
