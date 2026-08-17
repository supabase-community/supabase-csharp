using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Supabase.Storage;

public class BucketUpsertOptions
{
    /// <summary>
    /// The visibility of the bucket. Public buckets don't require an authorization token to download objects,
    /// but still require a valid token for all other operations. By default, buckets are private.
    /// </summary>
    [JsonPropertyName("public")]
    public bool Public { get; set; } = false;

    /// <summary>
    /// Specifies the file size limit that this bucket can accept during upload.
    ///
    /// Expects a string value following a format like: '1kb', '50mb', '150kb', etc.
    /// </summary>
    [JsonPropertyName("file_size_limit")]
    public string? FileSizeLimit { get; set; }

    /// <summary>
    /// Specifies the allowed mime types that this bucket can accept during upload.
    ///
    /// Expects a List of values such as: ['image/jpeg', 'image/png', etc]
    /// </summary>
    [JsonPropertyName("allowed_mime_types")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AllowedMimes { get; set; }
}
