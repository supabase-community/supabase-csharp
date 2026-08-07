#region

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;

#endregion

namespace Gotrue.Tests.Users;

/// <summary>
///     Covers <see cref="User.IsConfirmed" />: a user counts as confirmed when any of the confirmation
///     timestamps is set, so an auto-confirmed sign-up (which populates only <c>email_confirmed_at</c> or
///     <c>phone_confirmed_at</c>, not <c>confirmed_at</c>) is recognised as confirmed (issue #130).
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class UserConfirmationTests
{
    [TestMethod]
    [DataRow(true, false, false)]
    [DataRow(false, true, false)]
    [DataRow(false, false, true)]
    public void IsConfirmed_ShouldBeTrue_GivenAnyConfirmationTimestamp(bool confirmed, bool email, bool phone) =>
        UserWith(confirmed, email, phone).IsConfirmed.Should().BeTrue();

    [TestMethod]
    public void IsConfirmed_ShouldBeFalse_GivenNoConfirmationTimestamp() =>
        UserWith(false, false, false).IsConfirmed.Should().BeFalse(
            "an unconfirmed user has none of the confirmation timestamps set");

    private static User UserWith(bool confirmed, bool email, bool phone) =>
        new()
        {
            ConfirmedAt = confirmed ? DateTime.UtcNow : null,
            EmailConfirmedAt = email ? DateTime.UtcNow : null,
            PhoneConfirmedAt = phone ? DateTime.UtcNow : null,
        };
}
