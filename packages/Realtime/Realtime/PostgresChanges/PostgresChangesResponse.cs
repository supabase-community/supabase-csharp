using System.Collections.Generic;
using Newtonsoft.Json;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Models;
using Supabase.Realtime.Socket;

namespace Supabase.Realtime.PostgresChanges;

/// <inheritdoc />
public class PostgresChangesResponse<T> : SocketResponse<PostgresChangesPayload<T>> where T : class
{
    /// <inheritdoc />
    public PostgresChangesResponse(JsonSerializerSettings serializerSettings) : base(serializerSettings)
    {
    }
}

/// <summary>
/// A postgres changes event.
/// </summary>
public class PostgresChangesResponse : SocketResponse<PostgresChangesPayload<SocketResponsePayload>>
{
    /// <inheritdoc />
    public PostgresChangesResponse(JsonSerializerSettings serializerSettings) : base(serializerSettings)
    {
    }

    /// <summary>
    /// Postgrest client set by <see cref="Supabase.Realtime.RealtimeChannel"/> from
    /// <see cref="Supabase.Realtime.ClientOptions.PostgrestClient"/>, used to attach its context to models
    /// returned by <see cref="Model{TModel}"/>/<see cref="OldModel{TModel}"/>.
    /// </summary>
    internal IPostgrestClient? PostgrestClient { get; set; }

    /// <summary>
    /// Hydrates the referenced record into a Model (if possible).
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <returns></returns>
    public virtual TModel? Model<TModel>() where TModel : BaseModel, new()
    {
        if (Json != null && Payload != null && Payload.Data?.Record != null)
        {
            var response = JsonConvert.DeserializeObject<PostgresChangesResponse<TModel>>(Json, SerializerSettings);
            var model = response?.Payload?.Data?.Record;
            if (model != null)
                PostgrestClient?.Attach(model);
            return model;
        }
        else
        {
            return default;
        }
    }

    /// <summary>
    /// Hydrates the old_record into a Model (if possible).
    ///
    /// NOTE: If you want to receive the "previous" data for updates and deletes, you will need to set `REPLICA IDENTITY to FULL`, like this: `ALTER TABLE your_table REPLICA IDENTITY FULL`;
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <returns></returns>
    public virtual TModel? OldModel<TModel>() where TModel : BaseModel, new()
    {
        if (Json != null && Payload != null && Payload.Data?.OldRecord != null)
        {
            var response = JsonConvert.DeserializeObject<PostgresChangesResponse<TModel>>(Json, SerializerSettings);
            var model = response?.Payload?.Data?.OldRecord;
            if (model != null)
                PostgrestClient?.Attach(model);
            return model;
        }
        else
        {
            return default;
        }
    }
}

/// <summary>
/// The payload.
/// </summary>
/// <typeparam name="T"></typeparam>
public class PostgresChangesPayload<T> where T : class
{
    /// <summary>
    /// The payload data.
    /// </summary>
    [JsonProperty("data")]
    public SocketResponsePayload<T>? Data { get; set; }
    
    [JsonProperty("ids")]
    public List<int?> Ids { get; set; }
}