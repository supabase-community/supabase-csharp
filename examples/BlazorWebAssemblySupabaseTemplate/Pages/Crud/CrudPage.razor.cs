using BlazorWebAssemblySupabaseTemplate.Dtos;
using MudBlazor;

namespace BlazorWebAssemblySupabaseTemplate.Pages.Crud;

public partial class CrudPage
{
    protected override async Task OnInitializedAsync()
    {
        await GetTable();
    }

    // ---------------- SELECT TABLE
    private IReadOnlyList<Todo>? TodoList { get; set; }
    private IReadOnlyList<Todo>? TodoListFiltered { get; set; }
    private MudTable<Todo>? _table;
    protected async Task GetTable()
    {
        // await Task.Delay(10000);
        IReadOnlyList<Todo> todos = await DatabaseService.From<Todo>();
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
    private async Task OnClickDelete(Todo item)
    {
        await DatabaseService.SoftDelete(item);

        await GetTable();
    }

    // ---------------- CREATE NEW

    protected Todo Model = new();
    private bool _success = false;
    string[] _errors = { };
    MudForm? _form;
    private bool _processingNewItem = false;
    private async Task OnClickSave()
    {
        _processingNewItem = true;
        await DatabaseService.Insert<Todo>(Model);
        Model = new();
        await GetTable();
        _success = false;
        _processingNewItem = false;
    }
}