using RealtimeExample.Models;
using Supabase.Realtime;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.PostgresChanges;
using Supabase.Realtime.Socket;
using static Supabase.Realtime.Constants;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace RealtimeExample
{
    class Program
    {
        private const string ApiKey =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiIiLCJpYXQiOjE2NzEyMzc4NzMsImV4cCI6MjAwMjc3Mzk5MywiYXVkIjoiIiwic3ViIjoiIiwicm9sZSI6ImF1dGhlbnRpY2F0ZWQifQ.qoYdljDZ9rjfs1DKj5_OqMweNtj7yk20LZKlGNLpUO8";

        private const string SocketEndpoint = "ws://realtime-dev.localhost:4000/socket";

        static async Task Main(string[] args)
        {
            // Connect to db and web socket server
            var postgrestClient = new Supabase.Postgrest.Client("http://localhost:3000");
            var realtimeClient = new Client(SocketEndpoint, new ClientOptions
            {
                Parameters = new SocketOptionsParameters
                {
                    ApiKey = ApiKey
                }
            });

            realtimeClient.AddDebugHandler((sender, message, exception) => Console.WriteLine(message));
            realtimeClient.AddStateChangedHandler(SocketEventHandler);

            await realtimeClient.ConnectAsync();

            // Subscribe to a channel and events
            var channelTodos = realtimeClient.Channel("public:todos");
            channelTodos.Register(new PostgresChangesOptions("public", "todos"));
            channelTodos.AddPostgresChangeHandler(ListenType.Inserts, PostgresInsertedHandler);
            channelTodos.AddPostgresChangeHandler(ListenType.Updates, PostgresUpdatedHandler);
            channelTodos.AddPostgresChangeHandler(ListenType.Deletes, PostgresDeletedHandler);
            await channelTodos.Subscribe();

            Console.ReadKey();
        }

        private static void PostgresDeletedHandler(IRealtimeChannel _, PostgresChangesResponse change)
        {
            Console.WriteLine($"Item Deleted");
        }

        private static void PostgresUpdatedHandler(IRealtimeChannel _, PostgresChangesResponse change)
        {
            Console.WriteLine($"Item Updated: {change.Model<User>()}");
        }

        private static void PostgresInsertedHandler(IRealtimeChannel _, PostgresChangesResponse change)
        {
            Console.WriteLine($"New item inserted: {change.Model<User>()}");
        }

        private static void SocketEventHandler(IRealtimeClient<RealtimeSocket, RealtimeChannel> sender,
            SocketState state)
        {
            Debug.WriteLine($"Socket is ${state.ToString()}");
        }
    }
}