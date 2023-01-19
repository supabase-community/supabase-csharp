using BlazorWebAssemblySupabaseTemplate.Dtos;
using BlazorWebAssemblySupabaseTemplate.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Supabase.Gotrue;

namespace BlazorWebAssemblySupabaseTemplate.Pages.CrudPrivate;

public partial class CrudPagePrivate
{
    [Inject]
    protected AuthService AuthService { get; set; }
    [Inject]
    protected IDialogService DialogService { get; set; }

    protected User? UserLoggedIn { get; set; }

    protected override async Task OnInitializedAsync()
    {
        UserLoggedIn = await AuthService.GetUser();
        await GetTable();
    }

    // ---------------- SELECT TABLE
    private IReadOnlyList<TodoPrivate>? _todoList { get; set; }
    private IReadOnlyList<TodoPrivate>? _todoListFiltered { get; set; }
    private MudTable<TodoPrivate>? table;
    protected async Task GetTable()
    {
        // await Task.Delay(10000);
        IReadOnlyList<TodoPrivate> todos = await DatabaseService.From<TodoPrivate>();
        _todoList = todos;
        _todoListFiltered = todos;
        await InvokeAsync(StateHasChanged);
    }

    // ---------------- SEARCH
    private void OnValueChangedSearch(string text)
    {
        _todoListFiltered = _todoList?.Where(row => row.Title.Contains(text)).ToList();
    }

    // ---------------- DELETE
    private async Task OnClickDelete(TodoPrivate item)
    {
        if (UserLoggedIn is not null)
        {
            await DatabaseService.SoftDelete(item);
            await GetTable();
        }
        else
        {
            await DialogService.ShowMessageBox(
                "Warning",
                "You need to be logged In to create or change an item in this table."
            );
        }
    }

    // ---------------- CREATE NEW

    protected TodoPrivate model = new();
    private bool success = false;
    string[] errors = { };
    MudForm? form;
    private bool _processingNewItem = false;
    private async Task OnClickSave()
    {
        if (UserLoggedIn is not null && UserLoggedIn?.Id is not null)
        {
            // model.User_id = UserLoggedIn?.Id;
            model.User_id = "f05fad82-1d40-4a73-ae19-a325c73048f1";

            _processingNewItem = true;
            try
            {
                await DatabaseService.Insert<TodoPrivate>(model);
                model = new();
                await GetTable();
            }
            catch (System.Exception ex)
            {
                await DialogService.ShowMessageBox(
                    "Warning",
                    "This request was not completed because of some problem. Error message: "
                    +ex.Message
                );
            }
            success = false;
            _processingNewItem = false;
        }
        else
        {
            await DialogService.ShowMessageBox(
                "Warning",
                "You need to be logged In to create or change an item in this table."
            );
        }
    }
}