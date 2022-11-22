using Blazored.LocalStorage;
using Supabase.Gotrue;
using Supabase.Interfaces;

namespace BlazorWebAssemblySupabaseTemplate.Providers;

public class CustomSupabaseSessionHandler : ISupabaseSessionHandler
{
    private readonly ILocalStorageService localStorage;
    private readonly ILogger<CustomSupabaseSessionHandler> logger;

    public CustomSupabaseSessionHandler(
        ILocalStorageService localStorage,
        ILogger<CustomSupabaseSessionHandler> logger
    )
    {
        logger.LogInformation("------------------- CONSTRUCTOR -------------------");
        this.localStorage = localStorage;
        this.logger = logger;
    }

    public async Task<bool> SessionDestroyer()
    {
        logger.LogInformation("------------------- SessionDestroyer -------------------");
        await localStorage.RemoveItemAsync("SUPABASE_SESSION"); 
        return true;
    }

    public async Task<bool> SessionPersistor<TSession>(TSession session) where TSession : Session
    {
        logger.LogInformation("------------------- SessionPersistor -------------------");
        await localStorage.SetItemAsync("SUPABASE_SESSION", session);
        return true;
    }

    public async Task<TSession?> SessionRetriever<TSession>() where TSession : Session
    {
        logger.LogInformation("------------------- SessionRetriever -------------------");
        return (TSession?) await localStorage.GetItemAsync<Session>("SUPABASE_SESSION");
    }
}