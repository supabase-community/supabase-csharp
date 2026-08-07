using System.Collections.Generic;
using System.Linq;

namespace Supabase.Storage;

/// <summary>
///     Represents a container for HTTP headers, providing functionality for adding and retrieving headers.
/// </summary>
public class Header
{
    private readonly Dictionary<string, string> _headers = [];

    /// <summary>
    ///     Adds a new header to the collection or updates the value of an existing header.
    ///     Key will be lowercased
    /// </summary>
    /// <param name="key">The key of the header to add or update.</param>
    /// <param name="value">The value associated with the header key.</param>
    public void Add(string key, string value)
    {
        var newKey = key.ToLower();
        foreach (var header in _headers.Where(header => header.Key.ToLower() == newKey))
            _headers.Remove(header.Key);

        _headers.Add(newKey, value);
    }

    /// <summary>
    ///     Adds multiple headers to the collection or updates the values of existing headers.
    ///     Key will be lowercased
    /// </summary>
    /// <param name="headers">
    ///     A dictionary containing the headers to add or update, where the key is the header name and the
    ///     value is the header value.
    /// </param>
    public void Add(Dictionary<string, string> headers)
    {
        foreach (var header in headers)
            Add(header.Key, header.Value);
    }

    /// <summary>
    ///     Retrieves all the headers in the collection.
    /// </summary>
    /// <returns>A dictionary containing all headers, where the key is the header name and the value is the header value.</returns>
    public Dictionary<string, string> Get()
    {
        return _headers;
    }
}
