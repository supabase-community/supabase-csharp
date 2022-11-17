using BlazorWebAssemblySupabaseTemplate.Dtos;
using BlazorWebAssemblySupabaseTemplate.Services;
using MudBlazor;

namespace BlazorWebAssemblySupabaseTemplate.Pages.Crud;

public partial class CrudPage
{
    protected override async Task OnInitializedAsync()
    {
        await GetTable();
    }

    // ---------------- SELECT TABLE
    private IReadOnlyList<Todo>? _todoList { get; set; }
    private IReadOnlyList<Todo>? _todoListFiltered { get; set; }
    private MudTable<Todo>? table;
    protected async Task GetTable()
    {
        // await Task.Delay(10000);
        IReadOnlyList<Todo> todos = await DatabaseService.From<Todo>();
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
    private async Task OnClickDelete(Todo item)
    {
        await DatabaseService.Delete<Todo>(item);        
        await GetTable();
    }

    // ---------------- CREATE NEW

    protected Todo model = new();
    private bool success = false;
    string[] errors = { };
    MudForm? form;
    private bool _processingNewItem = false;
    private async Task OnClickSave()
    {
        _processingNewItem = true;
        await DatabaseService.Insert<Todo>(model);
        model = new();
        await GetTable();
        success = false;
        _processingNewItem = false;
    }
}