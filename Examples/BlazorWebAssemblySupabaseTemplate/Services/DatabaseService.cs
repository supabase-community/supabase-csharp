using System.Net;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Postgrest;

namespace BlazorWebAssemblySupabaseTemplate.Services;

public class DatabaseService
{
	private readonly Supabase.Client client;
	private readonly AuthenticationStateProvider customAuthStateProvider;
	private readonly ILocalStorageService localStorage;
	private readonly ILogger<DatabaseService> logger;
    private readonly IDialogService DialogService;

    public DatabaseService(
        Supabase.Client client,
        AuthenticationStateProvider CustomAuthStateProvider,
        ILocalStorageService localStorage,
        ILogger<DatabaseService> logger,
        IDialogService dialogService) : base()
    {
        logger.LogInformation("------------------- CONSTRUCTOR -------------------");

        this.client = client;
        customAuthStateProvider = CustomAuthStateProvider;
        this.localStorage = localStorage;
        this.logger = logger;
        DialogService = dialogService;
    }

    public async Task<IReadOnlyList<TModel>> From<TModel>() where TModel : BaseModelApp, new()
	{
		Postgrest.Responses.ModeledResponse<TModel> modeledResponse = await client
			.From<TModel>()
			.Where(x => x.SoftDeleted == false)
			.Get();
		return modeledResponse.Models;
	}

	public async Task<List<TModel>> Delete<TModel>(TModel item) where TModel : BaseModelApp, new()
	{
		Postgrest.Responses.ModeledResponse<TModel> modeledResponse = await client
			.From<TModel>()
			.Delete(item);
		return modeledResponse.Models;
	}

	public async Task<List<TModel>?> Insert<TModel>(TModel item) where TModel : BaseModelApp, new()
	{
		Postgrest.Responses.ModeledResponse<TModel> modeledResponse;
		try
		{
			modeledResponse = await client
				.From<TModel>()
				.Insert(item);			
			
			return modeledResponse.Models;
		}
		catch (RequestException ex)
		{
			if(ex.Response?.StatusCode == HttpStatusCode.Forbidden)
				await DialogService.ShowMessageBox(
					"Warning",
					"This database resquest was forbidden."
				);
			else		
				await DialogService.ShowMessageBox(
					"Warning",
					"This request was not completed because of some problem with the http request. \n "
					+ex.Response?.RequestMessage
				);
		}
		
		return null;		
	}

	public async Task<List<TModel>> SoftDelete<TModel>(TModel item) where TModel : BaseModelApp, new()
    {
        Postgrest.Responses.ModeledResponse<TModel> modeledResponse = await client.Postgrest
			.Table<TModel>()
            .Set(x => x.SoftDeleted, true)
            .Set(x => x.SoftDeletedAt, DateTime.Now)
            .Where(x => x.Id == item.Id)
            // .Filter(x => x.Id, Operator.Equals, item.Id)
            .Update();
        return modeledResponse.Models;
    }

}
