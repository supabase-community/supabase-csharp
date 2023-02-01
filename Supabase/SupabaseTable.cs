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
using Supabase.Realtime.Socket;
using static Supabase.Client;

namespace Supabase
{
    public class SupabaseTable<TModel> : Table<TModel>, ISupabaseTable<TModel, RealtimeChannel>
        where TModel : BaseModel, new()
    {
        private RealtimeChannel? Channel;

        private IPostgrestClient postgrestClient;

        private IRealtimeClient<RealtimeSocket, RealtimeChannel> realtimeClient;

        private string schema;

        public SupabaseTable(IPostgrestClient postgrestClient, IRealtimeClient<RealtimeSocket, RealtimeChannel> realtimeClient, string schema = "public") : base(postgrestClient.BaseUrl, Postgrest.Client.SerializerSettings(postgrestClient.Options), postgrestClient.Options)
        {
            this.postgrestClient = postgrestClient;
            GetHeaders = postgrestClient.GetHeaders;

            this.realtimeClient = realtimeClient;
            this.schema = schema;
        }

        public async Task<RealtimeChannel> On(ChannelEventType e, Action<object, PostgresChangesEventArgs> action)
        {
            if (Channel == null)
            {
                var parameters = new Dictionary<string, string>();

                // In regard to: https://github.com/supabase/supabase-js/pull/270
                var headers = postgrestClient?.GetHeaders?.Invoke();
                if (headers != null && headers.ContainsKey("Authorization"))
                {
                    parameters.Add("user_token", headers["Authorization"].Split(' ')[1]);
                }

                Channel = realtimeClient.Channel("realtime", schema, TableName, parameters: parameters);
            }

            if (realtimeClient.Socket == null || !realtimeClient.Socket.IsConnected)
                await realtimeClient.ConnectAsync();

            switch (e)
            {
                case ChannelEventType.Insert:
                    Channel.OnInsert += (sender, args) => action?.Invoke(sender, args);
                    break;
                case ChannelEventType.Update:
                    Channel.OnUpdate += (sender, args) => action?.Invoke(sender, args);
                    break;
                case ChannelEventType.Delete:
                    Channel.OnDelete += (sender, args) => action?.Invoke(sender, args);
                    break;
                case ChannelEventType.All:
                    Channel.OnPostgresChange += (sender, args) => action?.Invoke(sender, args);
                    break;
            }

            try
            {
                await Channel.Subscribe();
            }
            catch { }

            return Channel;
        }
    }
}
