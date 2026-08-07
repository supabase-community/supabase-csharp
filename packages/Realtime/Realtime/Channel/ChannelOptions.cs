using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Supabase.Realtime.Channel;

/// <summary>
/// Represents configuration options for a Realtime channel.
/// </summary>
/// <remarks>
/// This class contains all the necessary configuration options for establishing and maintaining
/// a Realtime channel connection, including authentication, parameters, and serialization settings.
/// </remarks>
public class ChannelOptions
{
    /// <summary>
    /// A function that returns the current access token.
    /// </summary>
    public Func<string?> RetrieveAccessToken { get; private set; }

    /// <summary>
    /// Parameters that are sent to the channel when opened (JSON Serializable)
    /// </summary>
    public Dictionary<string, string>? Parameters { get; set; }

    /// <summary>
    /// The Client Options
    /// </summary>
    public ClientOptions ClientOptions { get; }

    /// <summary>
    /// The Serializer Settings
    /// </summary>
    public JsonSerializerSettings SerializerSettings { get; }

    /// <summary>
    /// Whether the channel is private, i.e. authorized against the server's Row Level Security
    /// policies. Private channels are required for broadcast replay.
    /// </summary>
    public bool IsPrivate { get; }

    /// <summary>
    /// The Channel Options (typically only called from within the <see cref="Client"/>). Creates
    /// options for a public channel; use <see cref="Private"/> for a private one.
    /// </summary>
    /// <param name="clientOptions">The client configuration options.</param>
    /// <param name="retrieveAccessToken">A function that returns the current access token.</param>
    /// <param name="serializerSettings">The JSON serializer settings to be used for message serialization.</param>
    public ChannelOptions(
        ClientOptions clientOptions,
        Func<string?> retrieveAccessToken,
        JsonSerializerSettings serializerSettings
    ) : this(clientOptions, retrieveAccessToken, serializerSettings, false)
    {
    }

    private ChannelOptions(
        ClientOptions clientOptions,
        Func<string?> retrieveAccessToken,
        JsonSerializerSettings serializerSettings,
        bool isPrivate
    )
    {
        ClientOptions = clientOptions;
        SerializerSettings = serializerSettings;
        RetrieveAccessToken = retrieveAccessToken;
        IsPrivate = isPrivate;
    }

    /// <summary>
    /// Creates options for a public channel.
    /// </summary>
    /// <param name="clientOptions">The client configuration options.</param>
    /// <param name="retrieveAccessToken">A function that returns the current access token.</param>
    /// <param name="serializerSettings">The JSON serializer settings to be used for message serialization.</param>
    public static ChannelOptions Public(
        ClientOptions clientOptions,
        Func<string?> retrieveAccessToken,
        JsonSerializerSettings serializerSettings
    ) => new(clientOptions, retrieveAccessToken, serializerSettings, false);

    /// <summary>
    /// Creates options for a private channel, i.e. one authorized against the server's Row Level
    /// Security policies. Required for broadcast replay.
    /// </summary>
    /// <param name="clientOptions">The client configuration options.</param>
    /// <param name="retrieveAccessToken">A function that returns the current access token.</param>
    /// <param name="serializerSettings">The JSON serializer settings to be used for message serialization.</param>
    public static ChannelOptions Private(
        ClientOptions clientOptions,
        Func<string?> retrieveAccessToken,
        JsonSerializerSettings serializerSettings
    ) => new(clientOptions, retrieveAccessToken, serializerSettings, true);
}
