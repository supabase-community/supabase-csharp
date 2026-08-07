#region

using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using static Gotrue.Tests.TestUtils;
using static Supabase.Gotrue.StatelessClient;

#endregion

namespace Gotrue.Tests.Stateless;

/// <summary>
///     Base fixture for the stateless-client E2E tests: the <c>StatelessClient</c> holds no session, so every
///     call takes an options bag (and admin calls take a service-role key). This fixture supplies both, pointed
///     at the live stack.
/// </summary>
public abstract class StatelessFixture
{
    protected IGotrueStatelessClient<User, Session> Client { get; private set; } = null!;

    protected static StatelessClientOptions Options => TestClients.StatelessAgainstCliStack();

    protected static string ServiceRoleKey => GenerateServiceRoleToken(TestClients.CliJwtSecret);

    [TestInitialize]
    public void InitializeClient() => Client = new StatelessClient();
}
