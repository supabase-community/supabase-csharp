using Newtonsoft.Json;
using Supabase.Realtime.Interfaces;
using static Supabase.Realtime.Constants;

namespace Supabase.Realtime.Socket;

/// <summary>
/// A SocketResponse with support for Generically typed Payload
/// </summary>
/// <typeparam name="T"></typeparam>
public class SocketResponse<T> : SocketResponse where T : class
{
	/// <inheritdoc />
	public SocketResponse(JsonSerializerSettings serializerSettings) : base(serializerSettings)
	{ }

	/// <summary>
	/// The typed payload response
	/// </summary>
	[JsonProperty("payload")]
	public new T? Payload { get; set; }
}

/// <summary>
/// Representation of a Socket Response.
/// </summary>
public class SocketResponse : IRealtimeSocketResponse
{
	internal JsonSerializerSettings SerializerSettings;

	/// <summary>
	/// Represents a socket response
	/// </summary>
	/// <param name="serializerSettings"></param>
	public SocketResponse(JsonSerializerSettings serializerSettings)
	{
		SerializerSettings = serializerSettings;
	}

	/// <summary>
	/// The internal realtime topic.
	/// </summary>
	[JsonProperty("topic")]
	public string? Topic { get; set; }

	/// <summary>
	/// The internal, raw event given by the socket
	/// </summary>
	[JsonProperty("event")]
	public string? _event { get; set; }

	/// <summary>
	/// The typed, parsed event given by this library. 
	/// </summary>
	[JsonIgnore]
	public EventType Event
	{
		get
		{
			return _event switch
			{
				ChannelEventPresenceState => EventType.PresenceState,
				ChannelEventPresenceDiff => EventType.PresenceDiff,
				ChannelEventBroadcast => EventType.Broadcast,
				ChannelEventPostgresChanges => EventType.PostgresChanges,
				ChannelEventSystem => EventType.System,
				ChannelEventReply => EventType.PostgresChanges,
				_ => Payload?.Type ?? EventType.Unknown
			};
		}
	}

	/// <summary>
	/// The payload/response.
	/// </summary>
	[JsonProperty("payload")]
	public SocketResponsePayload? Payload { get; set; }

	/// <summary>
	/// An internal reference to this particular feedback loop.
	/// </summary>
	[JsonProperty("ref")]
	public string? Ref { get; set; }

	/// <summary>
	/// The raw JSON string of the received data.
	/// </summary>
	[JsonIgnore]
	internal string? Json { get; set; }
}