using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Supabase.Realtime.Channel;

/// <summary>
/// Channel Options
/// </summary>
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
    /// The Channel Options (typically only called from within the <see cref="Client"/>)
    /// </summary>
    /// <param name="clientOptions"></param>
    /// <param name="retrieveAccessToken"></param>
    /// <param name="serializerSettings"></param>
    public ChannelOptions(ClientOptions clientOptions, Func<string?> retrieveAccessToken, JsonSerializerSettings serializerSettings)
    {
        ClientOptions = clientOptions;
        SerializerSettings = serializerSettings;
        RetrieveAccessToken = retrieveAccessToken;
    }
}