using BlazorWebAssemblySupabaseTemplate.Dtos;
using BlazorWebAssemblySupabaseTemplate.Services;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace BlazorWebAssemblySupabaseTemplate.Pages.FileUploadFolder;

public partial class FileUpload
{

    protected override async Task OnInitializedAsync()
    {
        await GetFilesFromBucket();
    }
    
    public List<Supabase.Storage.FileObject>? fileObjects;
    private async Task GetFilesFromBucket()
    {
        fileObjects = await StorageService.GetFilesFromBucket("userfiles");
    }

    static long maxFileSizeInMB = 15;
    long maxFileSize = 1024 * maxFileSizeInMB;
    private async Task UploadFilesAsync(IBrowserFile file)
    {
        Console.WriteLine("file.Name");
        Console.WriteLine(file.Name);

        try
        {
            Stream streamData = file.OpenReadStream(maxFileSize);            

            string filename = await StorageService.UploadFile("userfiles", streamData, file.Name);

            Snackbar.Add( "File uploaded: "+filename.Split("/").Last() );

            await GetFilesFromBucket();
            await InvokeAsync(StateHasChanged);
        }
        catch (System.IO.IOException ex)
        {
            Snackbar.Add( "Error: Max file size exceeded." );
        }
    }
}