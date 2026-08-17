using System.Collections.Generic;
using System.Text.Json.Serialization;
using Supabase.Realtime.Broadcast;
using Supabase.Realtime.PostgresChanges;
using Supabase.Realtime.Presence;

namespace Supabase.Realtime.Channel;

internal class JoinPush
{
    [JsonPropertyName("config")]
    public JoinPushConfig Config { get; private set; }

    private JoinPush(BroadcastOptions? broadcastOptions, PresenceOptions? presenceOptions, List<PostgresChangesOptions>? postgresChangesOptions, bool isPrivate)
    {
        this.Config = new JoinPushConfig
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
        [JsonPropertyName("broadcast")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BroadcastOptions? Broadcast { get; set; }

        [JsonPropertyName("presence")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PresenceOptions? Presence { get; set; }

        [JsonPropertyName("postgres_changes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<PostgresChangesOptions> PostgresChanges { get; set; } = new List<PostgresChangesOptions> { };

        [JsonPropertyName("private")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsPrivate { get; set; }
    }
}
