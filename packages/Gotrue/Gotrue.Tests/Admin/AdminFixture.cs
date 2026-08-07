#region

using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

#endregion

namespace Gotrue.Tests.Admin;

/// <summary>
///     Base fixture for the service-role (admin) E2E tests: provides a fresh admin client per test, pointed at
///     the live stack and authenticated with a generated service-role token.
/// </summary>
public abstract class AdminFixture
{
    protected IGotrueAdminClient<User> Admin { get; private set; } = null!;

    [TestInitialize]
    public void InitializeAdmin() => Admin = TestClients.AdminAgainstCliStack();
}
