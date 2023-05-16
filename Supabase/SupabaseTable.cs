using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Postgrest;
using Postgrest.Interfaces;
using Postgrest.Models;
using Supabase.Interfaces;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Client;

namespace Supabase
{
    /// <summary>
    /// A Supabase wrapper for a Postgrest Table.
    /// </summary>
    /// <typeparam name="TModel">Model that implements <see cref="BaseModel"/></typeparam>
    public class SupabaseTable<TModel> : Table<TModel>, ISupabaseTable<TModel, RealtimeChannel>
        where TModel : BaseModel, new()
    {
        private RealtimeChannel? _channel;

        private readonly IPostgrestClient _postgrestClient;

        private readonly IRealtimeClient<RealtimeSocket, RealtimeChannel> _realtimeClient;

        private readonly string _schema;

        /// <summary>
        /// A Supabase wrapper for a Postgrest table.
        /// </summary>
        /// <param name="postgrestClient"></param>
        /// <param name="realtimeClient"></param>
        /// <param name="schema"></param>
        public SupabaseTable(IPostgrestClient postgrestClient, IRealtimeClient<RealtimeSocket, RealtimeChannel> realtimeClient, string schema = "public") : base(postgrestClient.BaseUrl, Postgrest.Client.SerializerSettings(postgrestClient.Options), postgrestClient.Options)
        {
            _postgrestClient = postgrestClient;
            _realtimeClient = realtimeClient;
            _schema = schema;
            GetHeaders = postgrestClient.GetHeaders;
        }

        /// <inheritdoc />
        public async Task<RealtimeChannel> On(ChannelEventType eventType, Action<object, PostgresChangesEventArgs> action)
        {
            if (_channel == null)
            {
                var parameters = new Dictionary<string, string>();

                // In regard to: https://github.com/supabase/supabase-js/pull/270
                var headers = _postgrestClient?.GetHeaders?.Invoke();
                if (headers != null && headers.TryGetValue("Authorization", out var header))
                {
                    parameters.Add("user_token", header.Split(' ')[1]);
                }

                _channel = _realtimeClient.Channel("realtime", _schema, TableName, parameters: parameters);
            }

            if (_realtimeClient.Socket == null || !_realtimeClient.Socket.IsConnected)
                await _realtimeClient.ConnectAsync();

            switch (eventType)
            {
                case ChannelEventType.Insert:
                    _channel.OnInsert += (sender, args) => action?.Invoke(sender, args);
                    break;
                case ChannelEventType.Update:
                    _channel.OnUpdate += (sender, args) => action?.Invoke(sender, args);
                    break;
                case ChannelEventType.Delete:
                    _channel.OnDelete += (sender, args) => action?.Invoke(sender, args);
                    break;
                case ChannelEventType.All:
                    _channel.OnPostgresChange += (sender, args) => action?.Invoke(sender, args);
                    break;
            }

            try
            {
                await _channel.Subscribe();
            }
            catch
            {
                // ignored
            }

            return _channel;
        }
    }
}
