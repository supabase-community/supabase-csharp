using System;
using Newtonsoft.Json;
using Supabase.Core.Attributes;
using System.Collections.Generic;

namespace Supabase.Realtime.PostgresChanges;

/// <summary>
/// Handles a `postgres_changes` channel
/// 
/// For Example in the js client: 
/// 
///		const databaseFilter = {
///			schema: 'public',
///			table: 'messages',
///			filter: `room_id=eq.${channelId}`,
///			event: 'INSERT',
///		}
///	
/// Would translate to:
/// 
///		new PostgresChangesOptions("public", "messages", $"room_id=eq.{channelId}");
/// </summary>
public class PostgresChangesOptions
{
    /// <summary>
    /// Mapping of postgres changes listener types
    /// </summary>
    public enum ListenType
    {
        /// <summary>
        /// All event
        /// </summary>
        [MapTo("*")]
        All,
        /// <summary>
        /// INSERT events
        /// </summary>
        [MapTo("INSERT")]
        Inserts,
        /// <summary>
        /// UPDATE events
        /// </summary>
        [MapTo("UPDATE")]
        Updates,
        /// <summary>
        /// DELETE events
        /// </summary>
        [MapTo("DELETE")]
        Deletes
    }

    /// <summary>
    /// The schema for this listener, likely: `public`
    /// </summary>
    [JsonProperty("schema")]
    public string Schema { get; set; }

    /// <summary>
    /// The table for this listener, can be: `*` matching all tables in schema.
    /// </summary>
    [JsonProperty("table")]
    public string? Table { get; set; }

    /// <summary>
    /// The filter for this listener
    /// </summary>
    [JsonProperty("filter", NullValueHandling = NullValueHandling.Ignore)]
    public string? Filter { get; set; }

    /// <summary>
    /// The parameters passed to the server
    /// </summary>
    [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Parameters { get; set; }

    /// <summary>
    /// The stringified event listener type
    /// </summary>
    [JsonProperty("event")]
    public string Event => Core.Helpers.GetMappedToAttr(_listenType).Mapping!;
    
    private readonly ListenType _listenType;

    /// <summary>
    /// Postgres changes options.
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <param name="eventType"></param>
    /// <param name="filter"></param>
    /// <param name="parameters"></param>
    public PostgresChangesOptions(string schema, string? table = null, ListenType eventType = ListenType.All, string? filter = null, Dictionary<string, string>? parameters = null)
    {
        _listenType = eventType;
        Schema = schema;
        Table = table;
        Filter = filter;
        Parameters = parameters;
    }

    private bool Equals(PostgresChangesOptions other)
    {
        return _listenType == other._listenType && Schema == other.Schema && Table == other.Table && Filter == other.Filter;
    }

    /// <summary>
    /// Check if object are equals 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (obj.GetType() != GetType()) return false;
        return Equals((PostgresChangesOptions)obj);
    }

    /// <summary>
    /// Generate hash code
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)_listenType;
            hashCode = (hashCode * 397) ^ Schema.GetHashCode();
            hashCode = (hashCode * 397) ^ (Table != null ? Table.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Filter != null ? Filter.GetHashCode() : 0);
            return hashCode;
        }
    }
}