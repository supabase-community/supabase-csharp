using Blazored.LocalStorage;
using BlazorWebAssemblySupabaseTemplate.Dtos;
using Microsoft.AspNetCore.Components.Authorization;
using Postgrest.Models;
using Supabase.Gotrue;
using Supabase.Interfaces;
using Supabase.Realtime;
using Supabase.Storage;

namespace BlazorWebAssemblySupabaseTemplate.Services;

public class DatabaseService
{
    private readonly ISupabaseClient<User, Session, Socket, Channel, Bucket, FileObject> client;
    private readonly AuthenticationStateProvider customAuthStateProvider;
    private readonly ILocalStorageService localStorage;
    private readonly ILogger<DatabaseService> logger;

    public DatabaseService(
        ISupabaseClient<User, Session, Socket, Channel, Bucket, FileObject> client,
        AuthenticationStateProvider CustomAuthStateProvider, 
        ILocalStorageService localStorage,
        ILogger<DatabaseService> logger
    ) : base()
    {
        logger.LogInformation("CONSTRUCTOR: DatabaseService");

        this.client = client;
        customAuthStateProvider = CustomAuthStateProvider;
        this.localStorage = localStorage;
        this.logger = logger;

        if( client.Postgrest == null)
        {
            client.InitializeAsync();
        }
    }

    public async Task<IReadOnlyList<TModel>> From<TModel>() where TModel : BaseModel, new()
    {
        Postgrest.Responses.ModeledResponse<TModel> modeledResponse = await client.From<TModel>().Get();
        return modeledResponse.Models;
    }
    
    public async Task<List<TModel>> Delete<TModel>(TModel item) where TModel : BaseModel, new()
    {
        Postgrest.Responses.ModeledResponse<TModel> modeledResponse = await client.From<TModel>().Delete(item);
        return modeledResponse.Models;
    }
    
    public async Task<List<TModel>> Insert<TModel>(TModel item) where TModel : BaseModel, new()
    {
        Postgrest.Responses.ModeledResponse<TModel> modeledResponse = await client.From<TModel>().Insert(item);
        return modeledResponse.Models;
    }

}
