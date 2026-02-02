#nullable enable

using Microsoft.Extensions.Logging;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace GotrueExample;

/// <summary>
/// This is an example of how to handle session persistence using the helpers and interfaces provided
///
/// We typically would load the session from storage here if we want to persist things to disk
/// between application restarts. Depending on your use case you will need to implement SaveSession and LoadSession differently
/// to handle loading and saving the session in your applications cache
/// </summary>
/// <example>
/// Usage using dependency injection
/// <code>
///     services.AddSingleton<IGotrueSessionPersistence<Session>, ClientPersistence>();
///     var provider = Host.Run();
///     var client = provider.GetRequiredService<IGotrueClient<User, Session>>();
///     var sessionPersistence = provider.GetRequiredService<IGotrueSessionPersistence<Session>>();
///     client.SetPersistence(sessionPersistence);
/// </code>
/// </example>
public class ClientPersistence : IGotrueSessionPersistence<Session>
{
    private Session? _session;
    private ILogger<ClientPersistence> Logger { get; }

    public ClientPersistence(ILogger<ClientPersistence> logger)
    {
        Logger = logger;
        IGotruePersistenceListener<Session> persistenceListener = new PersistenceListener(this);
        persistenceListener.Persistence.LoadSession();
    }

    public void SaveSession(Session session)
    {
        _session = session;
    }

    public void DestroySession()
    {
        _session = null;
    }

    public Session? LoadSession()
    {
        return _session;
    }
}
