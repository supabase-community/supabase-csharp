using System;
using System.Collections.Generic;
using System.Linq;

namespace Supabase.Realtime;

internal static class Utils
{
    /// <summary>
    /// Simple method to form a query string (albeit poorly) from a dictionary.
    /// </summary>
    /// <param name="dict"></param>
    /// <returns></returns>
    public static string QueryString(IDictionary<string, string?> dict)
    {
        var list = new List<string>();
        foreach (var item in dict)
        {
            if (!String.IsNullOrEmpty(item.Value))
                list.Add(item.Key + "=" + item.Value);
        }
        return string.Join("&", list);
    }

    /// <summary>
    /// Generates a Channel topic string of format: `realtime{:schema?}{:table?}{:col.eq.:val?}`
    /// </summary>
    /// <param name="database"></param>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GenerateChannelTopic(string database, string? schema, string? table, string? col, string? value)
    {
        var list = new List<string?> { database, schema, table };
        string channel = string.Join(":", list.Where(s => !string.IsNullOrEmpty(s)));

        if (!string.IsNullOrEmpty(col) && !string.IsNullOrEmpty(value))
        {
            channel += $":{col}=eq.{value}";
        }

        return channel;
    }
}