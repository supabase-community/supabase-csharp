using System.Text.Json.Serialization;

namespace Supabase.Storage;

public class SearchOptions
{
    /// <summary>
    /// Number of files to be returned
    /// </summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Starting position of query
    /// </summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; } = 0;

    /// <summary>
    /// The search string to filter files by
    /// </summary>
    [JsonPropertyName("search")]
    public string Search { get; set; } = string.Empty;

    /// <summary>
    /// Column to sort by. Can be any colum inside of a <see cref="FileObject"/>
    /// </summary>
    [JsonPropertyName("sortBy")]
    public SortBy SortBy { get; set; } = new SortBy { Column = "name", Order = "asc" };
}
