using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Postgrest;
using Postgrest.Interfaces;
using Postgrest.Models;
using Supabase.Interfaces;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using static Supabase.Client;

namespace Supabase
{
    public class SupabaseTable<TModel> : Table<TModel>, ISupabaseTable<TModel, Channel>
        where TModel : BaseModel, new()
    {
        private Channel? channel;

        private IPostgrestClient postgrestClient;

        private IRealtimeClient<Socket, Channel> realtimeClient;

        private string schema;

        public SupabaseTable(IPostgrestClient postgrestClient, IRealtimeClient<Socket, Channel> realtimeClient, string schema = "public") : base(postgrestClient.BaseUrl, Postgrest.Client.SerializerSettings(postgrestClient.Options), postgrestClient.Options)
        {
            this.postgrestClient = postgrestClient;
            GetHeaders = postgrestClient.GetHeaders;

            this.realtimeClient = realtimeClient;
            this.schema = schema;
        }

        public async Task<Channel> On(ChannelEventType e, Action<object, SocketResponseEventArgs> action)
        {
            if (channel == null)
            {
                var parameters = new Dictionary<string, string>();

                // In regard to: https://github.com/supabase/supabase-js/pull/270
                var headers = postgrestClient?.GetHeaders?.Invoke();
                if (headers != null && headers.ContainsKey("Authorization"))
                {
                    parameters.Add("user_token", headers["Authorization"].Split(' ')[1]);
                }

                channel = realtimeClient.Channel("realtime", schema, TableName, parameters: parameters);
            }

            if (realtimeClient.Socket == null || !realtimeClient.Socket.IsConnected)
                await realtimeClient.ConnectAsync();

            switch (e)
            {
                case ChannelEventType.Insert:
                    channel.OnInsert += (sender, args) => action?.Invoke(sender, args);
                    break;
                case ChannelEventType.Update:
                    channel.OnUpdate += (sender, args) => action?.Invoke(sender, args);
                    break;
                case ChannelEventType.Delete:
                    channel.OnDelete += (sender, args) => action?.Invoke(sender, args);
                    break;
                case ChannelEventType.All:
                    channel.OnMessage += (sender, args) => action?.Invoke(sender, args);
                    break;
            }

            try
            {
                await channel.Subscribe();
            }
            catch { }

            return channel;
        }
    }
}
