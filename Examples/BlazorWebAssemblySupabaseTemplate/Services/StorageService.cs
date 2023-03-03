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

    public async Task<string> UploadFile(String bucketName, Stream streamData, String saveName)
    {
        var bucket = Storage.From(bucketName);

        byte[] bytesData = await StreamToBytesAsync(streamData);

        return await bucket.Upload(bytesData, saveName);
        // return await bucket.Upload(new Byte[] { 0x0, 0x0, 0x0 }, saveName);
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

}