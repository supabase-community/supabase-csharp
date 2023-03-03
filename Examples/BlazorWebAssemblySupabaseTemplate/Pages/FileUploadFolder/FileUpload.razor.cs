using BlazorWebAssemblySupabaseTemplate.Dtos;
using BlazorWebAssemblySupabaseTemplate.Services;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace BlazorWebAssemblySupabaseTemplate.Pages.FileUploadFolder;

public partial class FileUpload
{
    private async Task UploadFilesAsync(IBrowserFile file)
    {
        Console.WriteLine("file.Name");
        Console.WriteLine(file.Name);

        string filename = await StorageService.UploadFile("userfiles", file.OpenReadStream(), file.Name);

        Snackbar.Add( "File uploaded: "+filename.Split("/").Last() );
    }
}