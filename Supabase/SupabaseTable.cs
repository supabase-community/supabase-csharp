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

        public SupabaseTable() : base(Client.Instance.RestUrl, new Postgrest.ClientOptions { Headers = Client.Instance.GetAuthHeaders(), Schema = Client.Instance.Schema })
        {
            channel = Client.Instance.Realtime.Channel("realtime", Instance.Schema, TableName);
        }

        public async Task<Channel> On(ChannelEventType e, Action<object, SocketResponseEventArgs> action)
        {
            if (Instance.Realtime.Socket == null || !Instance.Realtime.Socket.IsConnected)
                await Instance.Realtime.Connect();

            switch (e)
            {
                case ChannelEventType.Insert:
                    channel.OnInsert += (sender, args) => action.Invoke(sender, args);
                    break;
                case ChannelEventType.Update:
                    channel.OnUpdate += (sender, args) => action.Invoke(sender, args);
                    break;
                case ChannelEventType.Delete:
                    channel.OnInsert += (sender, args) => action.Invoke(sender, args);
                    break;
                case ChannelEventType.All:
                    channel.OnMessage += (sender, args) => action.Invoke(sender, args);
                    break;
            }

            try
            {
                await channel.Subscribe();
            } catch { }

            return channel;
        }
    }
}
