using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using static Supabase.Realtime.Constants;

namespace Supabase.Realtime.Socket;

/// <inheritdoc />
public class SocketResponsePayload<T> : SocketResponsePayload where T : class
{
    /// <summary>
    /// The record referenced.
    /// </summary>
    [JsonPropertyName("record")]
    public new T? Record { get; set; }

    /// <summary>
    /// The previous state of the referenced record.
    /// </summary>
    [JsonPropertyName("old_record")]
    public new T? OldRecord { get; set; }
}

/// <summary>
/// A socket response payload.
/// </summary>
public class SocketResponsePayload
{
    #region Postgres Changes

    /// <summary>
    /// Displays Column information from the Database.
    /// 
    /// Will always be an array but can be empty
    /// </summary>
    [JsonPropertyName("columns")]
    public List<object>? Columns { get; set; }

    /// <summary>
    /// The timestamp of the commit referenced.
    /// 
    /// Will either be a string or null
    /// </summary>
    [JsonPropertyName("commit_timestamp")]
    public DateTimeOffset? CommitTimestamp { get; set; }

    /// <summary>
    /// The record referenced.
    /// 
    /// Will always be an object but can be empty.
    /// </summary>
    [JsonPropertyName("record")]
    public object? Record { get; set; }

    /// <summary>
    /// The previous state of the referenced record.
    /// 
    /// Will always be an object but can be empty.
    /// </summary>
    [JsonPropertyName("old_record")]
    public object? OldRecord { get; set; }

    /// <summary>
    /// The Schema affected.
    /// </summary>
    [JsonPropertyName("schema")]
    public string? Schema { get; set; }

    /// <summary>
    /// The Table affected.
    /// </summary>
    [JsonPropertyName("table")]
    public string? Table { get; set; }

    /// <summary>
    /// The action type performed (INSERT, UPDATE, DELETE, etc.)
    /// </summary>
    [JsonPropertyName("type")]
    public string? ActionType { get; set; }

    /// <summary>
    /// The parsed type.
    /// </summary>
    [JsonIgnore]
    public EventType Type
    {
        get
        {
            switch (this.ActionType)
            {
                case "INSERT":
                    return EventType.Insert;
                case "UPDATE":
                    return EventType.Update;
                case "DELETE":
                    return EventType.Delete;
            }

            return EventType.Unknown;
        }
    }

    /// <summary>
    /// Status of response
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// The unparsed response object
    /// </summary>
    [JsonPropertyName("response")]
    public object? Response { get; set; }

    #endregion

    /// <summary>
    /// Either null or an array of errors.
    /// See: https://github.com/supabase/walrus/#error-states
    /// </summary>
    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; set; }

    #region Presence

    /// <summary>
    /// Presence joins (parsed later)
    /// </summary>
    [JsonPropertyName("joins")]
    public object? Joins { get; set; }

    /// <summary>
    /// Presence leaves (parsed later)
    /// </summary>
    [JsonPropertyName("leaves")]
    public object? Leaves { get; set; }

    #endregion

    #region System Messages

    /// <summary>
    /// The channel (system)
    /// </summary>
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    /// <summary>
    /// The extension (system)
    /// </summary>
    [JsonPropertyName("extension")] public string? Extension { get; set; }

    /// <summary>
    /// The message (system)
    /// </summary>
    [JsonPropertyName("message")] public string? Message { get; set; }

    #endregion
}
