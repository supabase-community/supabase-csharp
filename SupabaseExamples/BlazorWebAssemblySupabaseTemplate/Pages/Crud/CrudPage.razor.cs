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
    private IReadOnlyList<Lista>? _listaList { get; set; }
    private IReadOnlyList<Lista>? _listaListFiltered { get; set; }
    private MudTable<Lista>? table;
    protected async Task GetTable()
    {
        // await Task.Delay(10000);
        IReadOnlyList<Lista> listas = await DatabaseService.From<Lista>();
        _listaList = listas;
        _listaListFiltered = listas;
        await InvokeAsync(StateHasChanged);
    }

    // ---------------- SEARCH
    private void OnValueChangedSearch(string text)
    {
        _listaListFiltered = _listaList?.Where(row => row.Titulo.Contains(text)).ToList();
    }
    
    // ---------------- DELETE
    private async Task OnClickDelete(Lista item)
    {
        await DatabaseService.Delete<Lista>(item);        
        await GetTable();
    }

    // ---------------- CREATE NEW

    protected Lista model = new();
    private bool success = false;
    string[] errors = { };
    MudForm? form;
    private bool _processingNewItem = false;
    private async Task OnClickSave()
    {
        _processingNewItem = true;
        await DatabaseService.Insert<Lista>(model);
        model = new();
        await GetTable();
        success = false;
        _processingNewItem = false;
    }
}