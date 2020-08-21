using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Supabase;
using SupabaseExample.Models;
using static Supabase.Realtime.Constants;

namespace SupabaseExample
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var client = Client.Initialize(Environment.GetEnvironmentVariable("SUPABASE_URL"), new ClientAuthorization(ClientAuthorization.AuthorizationType.ApiKey, Environment.GetEnvironmentVariable("SUPABASE_KEY")));

            await client.Auth().SignUp("user@example.com", "terriblepassword");
            var user = client.Auth().CurrentUser;

            var users = await client.Postgrest<User>().Get();

            client.Realtime<User>().On(EventType.Update, OnUserUpdate);

            var cts = new CancellationTokenSource();

            await Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                    var model = users.Models.Last();
                    model.Username = Guid.NewGuid().ToString();
                    await model.Update<User>();
                }
            }, cts.Token);

            return 0;
        }

        static void OnUserUpdate(User updated)
        {

        }
    }
}
