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
    private IReadOnlyList<TodoPrivate>? TodoList { get; set; }
    private IReadOnlyList<TodoPrivate>? TodoListFiltered { get; set; }
    private MudTable<TodoPrivate>? _table;
    protected async Task GetTable()
    {
        // await Task.Delay(10000);
        IReadOnlyList<TodoPrivate> todos = await DatabaseService.From<TodoPrivate>();
        TodoList = todos;
        TodoListFiltered = todos;
        await InvokeAsync(StateHasChanged);
    }

    // ---------------- SEARCH
    private void OnValueChangedSearch(string text)
    {
        TodoListFiltered = TodoList?.Where(row => row.Title.Contains(text)).ToList();
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

    protected TodoPrivate Model = new();
    private bool _success = false;
    string[] _errors = { };
    MudForm? _form;
    private bool _processingNewItem = false;
    private async Task OnClickSave()
    {
        if (UserLoggedIn is not null && UserLoggedIn?.Id is not null)
        {
            Console.WriteLine("UserLoggedIn?.Id");
            Console.WriteLine(UserLoggedIn?.Id);

            Model.UserId = UserLoggedIn?.Id;

            _processingNewItem = true;
            try
            {
                await DatabaseService.Insert<TodoPrivate>(Model);
                Model = new();
                await GetTable();
            }
            catch (Exception ex)
            {
                await DialogService.ShowMessageBox(
                    "Warning",
                    "This request was not completed because of some problem. Error message: "
                    +ex.Message
                );
            }
            _success = false;
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