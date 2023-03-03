using System.Net;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Postgrest;
using Storage.Interfaces;

namespace BlazorWebAssemblySupabaseTemplate.Services;

public class StorageService
{
    private readonly Supabase.Client client;
    private readonly ILogger<DatabaseService> logger;
    private readonly IDialogService dialogService;
    private readonly IStorageClient<Supabase.Storage.Bucket, Supabase.Storage.FileObject> Storage;

    public StorageService(
        Supabase.Client client,
        ILogger<DatabaseService> logger,
        IDialogService dialogService
    ) : base()
    {
        logger.LogInformation("------------------- CONSTRUCTOR -------------------");
        this.client = client;
        this.logger = logger;
        this.dialogService = dialogService;

        Storage = client.Storage;
    }

    public async Task<string> UploadFile(String bucketName, Stream streamData, String fileName)
    {
        var bucket = Storage.From(bucketName);

        // Maybe this isn't a good way to do it
        byte[] bytesData = await StreamToBytesAsync(streamData);

        string fileExtesion = fileName.Split(".").Last();

        String saveName = "File_" + DateTime.Now;

        saveName = saveName.Replace("/", "_").Replace(" ", "_").Replace(":", "_");
        saveName = saveName + "." + fileExtesion;

        // Console.WriteLine("saveName");
        // Console.WriteLine(saveName);

        return await bucket.Upload(bytesData, saveName);
    }

    public async Task<byte[]> StreamToBytesAsync(Stream streamData)
    {
        byte[] bytes;

        using (MemoryStream memoryStream = new MemoryStream())
        {
            await streamData.CopyToAsync(memoryStream);
            bytes = memoryStream.ToArray();
        }

        return bytes;
    }

    public async Task<List<Supabase.Storage.FileObject>?> GetFilesFromBucket(String bucketName)
    {
        return await Storage.From(bucketName).List();
    }
}