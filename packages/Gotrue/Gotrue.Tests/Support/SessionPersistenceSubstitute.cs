#region

using NSubstitute;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

#endregion

namespace Gotrue.Tests.Support;

/// <summary>
///     Builds an <see cref="IGotrueSessionPersistence{Session}" /> substitute that behaves like an in-memory
///     store: whatever the SDK saves is returned on the next load, and destroying clears it. Tests observe
///     the persisted session through <c>LoadSession()</c> (its public contract) rather than any test-only
///     field, keeping the double honest and free of hand-written stub classes (CONVENTIONS §5.6).
/// </summary>
internal static class SessionPersistenceSubstitute
{
    internal static IGotrueSessionPersistence<Session> Tracking()
    {
        var persistence = Substitute.For<IGotrueSessionPersistence<Session>>();
        Session? saved = null;
        persistence.When(p => p.SaveSession(Arg.Any<Session>())).Do(call => saved = call.Arg<Session>());
        persistence.When(p => p.DestroySession()).Do(_ => saved = null);
        persistence.LoadSession().Returns(_ => saved);
        return persistence;
    }
}
