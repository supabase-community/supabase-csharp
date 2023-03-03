using BlazorWebAssemblySupabaseTemplate.Dtos;
using BlazorWebAssemblySupabaseTemplate.Services;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace BlazorWebAssemblySupabaseTemplate.Pages.FileUploadFolder;

public partial class FileUpload
{
    IList<IBrowserFile> files = new List<IBrowserFile>();
    private async Task UploadFilesAsync(IBrowserFile file)
    {
        Console.WriteLine("file.Name");
        Console.WriteLine(file.Name);

        files.Add(file);

        // var fileContent = new StreamContent(file.OpenReadStream());

        string v = await StorageService.UploadFile("userfiles", file.OpenReadStream(), "teste.pdf");

        Console.WriteLine("v");
        Console.WriteLine(v);

    }
}