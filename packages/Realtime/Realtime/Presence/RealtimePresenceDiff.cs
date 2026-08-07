using Newtonsoft.Json;
using Supabase.Realtime.Models;
using Supabase.Realtime.Socket;
using System.Collections.Generic;

namespace Supabase.Realtime.Presence;

/// <summary>
/// Represents a presence_diff response
/// </summary>
/// <typeparam name="TPresence"></typeparam>
public class RealtimePresenceDiff<TPresence> : SocketResponse<PresenceDiffPayload<TPresence>> where TPresence : BasePresence
{
	/// <inheritdoc />
	public RealtimePresenceDiff(JsonSerializerSettings serializerSettings) : base(serializerSettings)
	{}
}

/// <summary>
/// a Presence Diff payload
/// </summary>
/// <typeparam name="TPresence"></typeparam>
public class PresenceDiffPayload<TPresence> where TPresence : BasePresence
{
	/// <summary>
	/// The joining presences.
	/// </summary>
	[JsonProperty("joins")]
	public Dictionary<string, PresenceDiffPayloadMeta<TPresence>>? Joins { get; set; }

	/// <summary>
	/// The leaving presences.
	/// </summary>
	[JsonProperty("leaves")]
	public Dictionary<string, PresenceDiffPayloadMeta<TPresence>>? Leaves { get; set; }
}

/// <summary>
/// A presence diff payload
/// </summary>
/// <typeparam name="TPresence"></typeparam>
public class PresenceDiffPayloadMeta<TPresence> where TPresence : BasePresence
{
	/// <summary>
	/// The metas containing current presences
	/// </summary>
	[JsonProperty("metas")]
	public List<TPresence>? Metas { get; set; }
}