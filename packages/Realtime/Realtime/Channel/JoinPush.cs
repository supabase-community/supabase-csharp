using System.Collections.Generic;
using Newtonsoft.Json;
using Supabase.Realtime.Broadcast;
using Supabase.Realtime.PostgresChanges;
using Supabase.Realtime.Presence;

namespace Supabase.Realtime.Channel;

internal class JoinPush
{
	[JsonProperty("config")]
	public JoinPushConfig Config { get; private set; }

	private JoinPush(BroadcastOptions? broadcastOptions, PresenceOptions? presenceOptions, List<PostgresChangesOptions>? postgresChangesOptions, bool isPrivate)
	{
		Config = new JoinPushConfig
		{
			Broadcast = broadcastOptions,
			Presence = presenceOptions,
			PostgresChanges = postgresChangesOptions ?? new List<PostgresChangesOptions>(),
			IsPrivate = isPrivate
		};
	}

	public static JoinPush ForPublicChannel(BroadcastOptions? broadcastOptions = null, PresenceOptions? presenceOptions = null, List<PostgresChangesOptions>? postgresChangesOptions = null)
		=> new(broadcastOptions, presenceOptions, postgresChangesOptions, isPrivate: false);

	public static JoinPush ForPrivateChannel(BroadcastOptions? broadcastOptions = null, PresenceOptions? presenceOptions = null, List<PostgresChangesOptions>? postgresChangesOptions = null)
		=> new(broadcastOptions, presenceOptions, postgresChangesOptions, isPrivate: true);

	internal class JoinPushConfig
	{
		[JsonProperty("broadcast", NullValueHandling = NullValueHandling.Ignore)]
		public BroadcastOptions? Broadcast { get; set; }

		[JsonProperty("presence", NullValueHandling = NullValueHandling.Ignore)]
		public PresenceOptions? Presence { get; set; }

		[JsonProperty("postgres_changes", NullValueHandling = NullValueHandling.Ignore)]
		public List<PostgresChangesOptions> PostgresChanges { get; set; } = new List<PostgresChangesOptions> { };

		[JsonProperty("private", NullValueHandling = NullValueHandling.Ignore)]
		public bool? IsPrivate { get; set; }
	}
}