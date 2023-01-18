using Blazored.LocalStorage;
using Supabase.Gotrue;
using Supabase.Interfaces;

namespace BlazorWebAssemblySupabaseTemplate.Providers;

public class CustomSupabaseSessionHandler : ISupabaseSessionHandler
{
    private readonly ILocalStorageService localStorage;
    private readonly ILogger<CustomSupabaseSessionHandler> logger;
    private static string SESSION_KEY = "SUPABASE_SESSION";

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
        await localStorage.RemoveItemAsync(SESSION_KEY);
        return true;
    }

    public async Task<bool> SessionPersistor<TSession>(TSession session) where TSession : Session
    {
        logger.LogInformation("------------------- SessionPersistor -------------------");
        await localStorage.SetItemAsync(SESSION_KEY, session);
        return true;
    }

    public async Task<TSession?> SessionRetriever<TSession>() where TSession : Session
    {
        logger.LogInformation("------------------- SessionRetriever -------------------");

        // Session session = await localStorage.GetItemAsync<Session>(SESSION_KEY);
        Session2 session = await localStorage.GetItemAsync<Session2>(SESSION_KEY); // work around

        Console.WriteLine("session.CreatedAt");
        Console.WriteLine(session?.CreatedAt);
        Console.WriteLine("session.ExpiresAt");
        Console.WriteLine(session?.ExpiresAt());

        if(session?.ExpiresAt() <= DateTime.Now)
            return null;
        else
            return (TSession?) await localStorage.GetItemAsync<Session>(SESSION_KEY);
    }

}

public class Session2
{
    public string? AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? RefreshToken { get; set; }
    public string? TokenType { get; set; }
    public User? User { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt() => new DateTime(CreatedAt.Ticks).AddSeconds(ExpiresIn);
}


