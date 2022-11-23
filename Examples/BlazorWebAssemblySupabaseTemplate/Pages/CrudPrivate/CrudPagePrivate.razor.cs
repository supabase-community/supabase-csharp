using BlazorWebAssemblySupabaseTemplate.Dtos;
using BlazorWebAssemblySupabaseTemplate.Services;
using MudBlazor;

namespace BlazorWebAssemblySupabaseTemplate.Pages.CrudPrivate;

public partial class CrudPagePrivate
{
    protected override async Task OnInitializedAsync()
    {
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
        await DatabaseService.Delete<TodoPrivate>(item);        
        await GetTable();
    }

    // ---------------- CREATE NEW

    protected TodoPrivate model = new();
    private bool success = false;
    string[] errors = { };
    MudForm? form;
    private bool _processingNewItem = false;
    private async Task OnClickSave()
    {
        string user_id = await localStorage.GetItemAsync<string>("user_id");
        
        model.User_id = user_id;
        _processingNewItem = true;
        await DatabaseService.Insert<TodoPrivate>(model);
        model = new();
        await GetTable();
        success = false;
        _processingNewItem = false;
    }
}