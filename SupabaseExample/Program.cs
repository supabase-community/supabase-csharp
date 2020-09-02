using System;
using System.Diagnostics;
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

            await client.Auth().Login("joseph@acupajoe.io", "testing");
            var user = client.Auth().CurrentUser;

            try
            {
                var customRecord = await client.Database<User>()
                    .Filter("username", Postgrest.Constants.Operator.Equals, user.Email)
                    .Single();

                if (customRecord != null)
                {
                    Debug.WriteLine($"{customRecord.Username}'s record was born on {customRecord.InsertedAt}");
                }
            }
            catch (Exception err)
            {
                var model = new User
                {
                    Username = user.Email,
                    Status = "active",
                    Catchphrase = "Gotta catch 'em all",
                    AgeRange = new Range(15, 30)
                };
                var result = await client.Database<User>().Insert(model);
            }

            return 0;
        }

        static void OnUserUpdate(User updated)
        {

        }
    }
}
