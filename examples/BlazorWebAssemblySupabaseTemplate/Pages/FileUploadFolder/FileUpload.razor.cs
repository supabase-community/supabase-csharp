using Microsoft.AspNetCore.Components.Forms;

namespace BlazorWebAssemblySupabaseTemplate.Pages.FileUploadFolder;

public partial class FileUpload
{
    protected override async Task OnInitializedAsync()
    {
        await GetFilesFromBucket();
    }
    
    public List<Supabase.Storage.FileObject>? FileObjects;
    private async Task GetFilesFromBucket()
    {
        FileObjects = await StorageService.GetFilesFromBucket("userfiles");
    }

    static long _maxFileSizeInMb = 15;
    long _maxFileSize = 1024 * 1024 * _maxFileSizeInMb;
    private async Task UploadFilesAsync(IBrowserFile file)
    {
        Console.WriteLine("file.Name");
        Console.WriteLine(file.Name);

        try
        {
            var streamData = file.OpenReadStream(_maxFileSize);            

            var filename = await StorageService.UploadFile("userfiles", streamData, file.Name);

            Snackbar.Add( "File uploaded: "+filename.Split("/").Last() );

            await GetFilesFromBucket();
            await InvokeAsync(StateHasChanged);
        }
        catch (IOException ex)
        {
            Snackbar.Add( "Error: Max file size exceeded." );
        }
    }

    private async Task DownloadClick(Supabase.Storage.FileObject row)
    {
        var result = await DialogService.ShowMessageBox(
            "Warning", 
            "The download feature is disabled because of security risks, but it could be tested with your own risk by downloading the source code and running it. "
            );

        // byte[] downloadedBytes = await StorageService.DownloadFile("userfiles", row.Name);

        // await JS.InvokeVoidAsync("downloadFileFromStream", row.Name, downloadedBytes);
    }
}