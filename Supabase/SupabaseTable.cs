using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Postgrest;
using Postgrest.Models;
using Supabase.Realtime;
using static Supabase.Client;

namespace Supabase
{
    public class SupabaseTable<T> : Table<T> where T : BaseModel, new()
    {
        private Channel channel;

        public SupabaseTable() : base(Client.Instance.RestUrl, new Postgrest.ClientOptions { Headers = Instance.GetAuthHeaders(), Schema = Instance.Schema })
        { }

        public SupabaseTable(string restUrl, Postgrest.ClientOptions options) : base(restUrl, options)
        { }

        public async Task<Channel> On(ChannelEventType e, Action<object, SocketResponseEventArgs> action)
        {
            if (channel == null)
            {
                var parameters = new Dictionary<string, string>();

                // In regard to: https://github.com/supabase/supabase-js/pull/270
                var headers = Instance.GetAuthHeaders();
                if (headers.ContainsKey("Authorization"))
                {
                    parameters.Add("user_token", headers["Authorization"].Split(' ')[1]);
                }

                channel = Instance.Realtime.Channel("realtime", Instance.Schema, TableName, parameters: parameters);
            }

            if (Instance.Realtime.Socket == null || !Instance.Realtime.Socket.IsConnected)
                await Instance.Realtime.ConnectAsync();

            switch (e)
            {
                case ChannelEventType.Insert:
                    channel.OnInsert += (sender, args) => action.Invoke(sender, args);
                    break;
                case ChannelEventType.Update:
                    channel.OnUpdate += (sender, args) => action.Invoke(sender, args);
                    break;
                case ChannelEventType.Delete:
                    channel.OnDelete += (sender, args) => action.Invoke(sender, args);
                    break;
                case ChannelEventType.All:
                    channel.OnMessage += (sender, args) => action.Invoke(sender, args);
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
