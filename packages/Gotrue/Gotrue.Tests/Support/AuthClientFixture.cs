#region

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using static Gotrue.Tests.TestUtils;
using static Supabase.Gotrue.Constants.AuthState;

#endregion

namespace Gotrue.Tests.Support;

/// <summary>
///     Base fixture for E2E tests that drive the stateful <c>Client</c> against the live stack. It provides a
///     fresh client per test wired to a tracking persistence double and an auth-state recorder, plus the
///     shared "what a signed-in / signed-out client looks like" assertions. Derived classes add only the
///     scenario under test.
/// </summary>
public abstract class AuthClientFixture
{
    protected readonly List<Constants.AuthState> StateChanges = new();
    protected IGotrueClient<User, Session> Client { get; private set; } = null!;
    protected IGotrueSessionPersistence<Session> Persistence { get; private set; } = null!;

    [TestInitialize]
    public void InitializeClient()
    {
        StateChanges.Clear();
        Persistence = SessionPersistenceSubstitute.Tracking();
        Client = TestClients.AgainstCliStack();
        Client.SetPersistence(Persistence);
        Client.AddStateChangedListener((_, state) => StateChanges.Add(state));
    }

    protected async Task<Session> SignUpNewUser()
    {
        var session = await Client.SignUp(RandomEmail(), Password);
        session.Should().NotBeNull();
        return session!;
    }

    protected void VerifyGoodSession(Session? session)
    {
        using (new AssertionScope())
        {
            session.Should().NotBeNull();
            StateChanges.Should().Contain(SignedIn);
            Persistence.LoadSession().Should().BeSameAs(Client.CurrentSession, "the SDK persists the session it signed in");
            Client.CurrentUser!.Id.Should().Be(session!.User!.Id);
            session.AccessToken.Should().NotBeNull();
            session.RefreshToken.Should().NotBeNull();
            session.User.Should().NotBeNull();
        }
    }

    protected void VerifySignedOut()
    {
        using (new AssertionScope())
        {
            StateChanges.Should().ContainSingle(state => state == SignedOut);
            Persistence.LoadSession().Should().BeNull();
            Client.CurrentSession.Should().BeNull();
            Client.CurrentUser.Should().BeNull();
        }
    }
}
