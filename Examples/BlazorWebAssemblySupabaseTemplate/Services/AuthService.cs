using Blazored.LocalStorage;
using BlazorWebAssemblySupabaseTemplate.Dtos;
using Microsoft.AspNetCore.Components.Authorization;
using Supabase.Gotrue;
using Supabase.Interfaces;
using Supabase.Realtime;
using Supabase.Storage;

namespace BlazorWebAssemblySupabaseTemplate.Services;

public class AuthService
{
    private readonly Supabase.Client client;
    private readonly AuthenticationStateProvider customAuthStateProvider;
    private readonly ILocalStorageService localStorage;
    private readonly ILogger<AuthService> logger;

    public AuthService(
        Supabase.Client client,
        AuthenticationStateProvider CustomAuthStateProvider, 
        ILocalStorageService localStorage,
        ILogger<AuthService> logger
    ) : base()
    {
        logger.LogInformation("------------------- CONSTRUCTOR -------------------");

        this.client = client;
        customAuthStateProvider = CustomAuthStateProvider;
        this.localStorage = localStorage;
        this.logger = logger;
    }

    public async Task Login(string email, string password)
    {
        logger.LogInformation("METHOD: Login");
        
        Session? session = await client.Auth.SignIn(email, password);

        logger.LogInformation("------------------- User logged in -------------------");
        // logger.LogInformation($"instance.Auth.CurrentUser.Id {client?.Auth?.CurrentUser?.Id}");
        logger.LogInformation($"client.Auth.CurrentUser.Email {client?.Auth?.CurrentUser?.Email}");

        await customAuthStateProvider.GetAuthenticationStateAsync();
    }
    
    public async Task Logout()
    {
        await client.Auth.SignOut();
        await localStorage.RemoveItemAsync("token");
        await customAuthStateProvider.GetAuthenticationStateAsync();
    }

    public async Task<User?> GetUser()
    {
        Session? session = await client.Auth.RetrieveSessionAsync();
        return session?.User;
    }

}
