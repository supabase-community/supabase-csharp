using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class DownloadOptions
    {
        /// <summary>
        ///    <p>Use the original file name when downloading</p>
        /// </summary>
        public static readonly DownloadOptions UseOriginalFileName = new DownloadOptions { FileName = "" };
        
        /// <summary>
        ///     <p>The name of the file to be downloaded</p>
        ///     <p>When field is null, no download attribute will be added.</p>
        ///     <p>When field is empty, the original file name will be used. Use <see cref="UseOriginalFileName"/> for quick initialized with original file names.</p>
        /// </summary>
        public string? FileName { get; set; }
    }
}