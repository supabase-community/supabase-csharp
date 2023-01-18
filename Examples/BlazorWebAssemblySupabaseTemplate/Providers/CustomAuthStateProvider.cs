using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

// Credits https://github.com/patrickgod/BlazorAuthenticationTutorial

namespace BlazorWebAssemblySupabaseTemplate.Providers;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly Supabase.Client _client;

    private readonly ILogger<CustomAuthStateProvider> _logger;

    public CustomAuthStateProvider(
        ILocalStorageService localStorage,
        Supabase.Client client,
        ILogger<CustomAuthStateProvider> logger
    )
    {
        logger.LogInformation("------------------- CONSTRUCTOR -------------------");

        _localStorage = localStorage;
        _client = client;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        _logger.LogInformation("------------------- GetAuthenticationStateAsync -------------------");

        // Sets client auth and connects to realtime (if enabled)
        await _client.InitializeAsync();

        var identity = new ClaimsIdentity();
        // _http.DefaultRequestHeaders.Authorization = null;

        if (!string.IsNullOrEmpty(_client.Auth.CurrentSession?.AccessToken))
        {
            identity = new ClaimsIdentity(ParseClaimsFromJwt(_client.Auth.CurrentSession.AccessToken), "jwt");
            // _http.DefaultRequestHeaders.Authorization =
            //     new AuthenticationHeaderValue("Bearer", token.Replace("\"", ""));
        }

        var user = new ClaimsPrincipal(identity);
        var state = new AuthenticationState(user);

        NotifyAuthenticationStateChanged(Task.FromResult(state));

        return state;
    }

    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
        return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()));
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
