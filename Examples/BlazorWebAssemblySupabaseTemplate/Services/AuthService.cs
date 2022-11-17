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
    private readonly ISupabaseClient<User, Session, Socket, Channel, Bucket, FileObject> client;
    private readonly AuthenticationStateProvider customAuthStateProvider;
    private readonly ILocalStorageService localStorage;
    private readonly ILogger<AuthService> logger;

    public AuthService(
        ISupabaseClient<User, Session, Socket, Channel, Bucket, FileObject> client,
        AuthenticationStateProvider CustomAuthStateProvider, 
        ILocalStorageService localStorage,
        ILogger<AuthService> logger
    ) : base()
    {
        logger.LogInformation("CONSTRUCTOR: AuthService");

        this.client = client;
        customAuthStateProvider = CustomAuthStateProvider;
        this.localStorage = localStorage;
        this.logger = logger;

        if( client.Auth == null)
        {
            client.InitializeAsync();
        }        
    }

    public async Task Login(string email, string password)
    {
        logger.LogInformation("METHOD: Login");
        
        Session? session = await client.Auth.SignIn(email, password);

        logger.LogInformation("------------------- User logged in -------------------");
        logger.LogInformation($"instance.Auth.CurrentUser.Id {client?.Auth?.CurrentUser?.Id}");
        logger.LogInformation($"client.Auth.CurrentUser.Email {client?.Auth?.CurrentUser?.Email}");
        
        await localStorage.SetItemAsStringAsync("token", session?.AccessToken);
        await customAuthStateProvider.GetAuthenticationStateAsync();
    }
    
    public async Task Logout()
    {
        await localStorage.RemoveItemAsync("token");
        await customAuthStateProvider.GetAuthenticationStateAsync();
    }

}
