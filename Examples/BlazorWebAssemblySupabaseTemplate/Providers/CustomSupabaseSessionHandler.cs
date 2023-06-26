using Blazored.LocalStorage;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace BlazorWebAssemblySupabaseTemplate.Providers;

public class CustomSupabaseSessionHandler : IGotrueSessionPersistence<Session>
{
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<CustomSupabaseSessionHandler> _logger;
    private const string SessionKey = "SUPABASE_SESSION";

    public CustomSupabaseSessionHandler(
        ILocalStorageService localStorage,
        ILogger<CustomSupabaseSessionHandler> logger
    )
    {
        logger.LogInformation("------------------- CONSTRUCTOR -------------------");
        _localStorage = localStorage;
        _logger = logger;
    }

    public async void DestroySession()
    {
        _logger.LogInformation("------------------- SessionDestroyer -------------------");
        await _localStorage.RemoveItemAsync(SessionKey);
    }

    public async void SaveSession(Session session)
    {
        _logger.LogInformation("------------------- SessionPersistor -------------------");
        await _localStorage.SetItemAsync(SessionKey, session);
    }

    public Session? LoadSession()
    {
        _logger.LogInformation("------------------- SessionRetriever -------------------");

        var session = _localStorage.GetItemAsync<Session>(SessionKey).Result;
        return session?.ExpiresAt() <= DateTime.Now ? null : session;
    }
}