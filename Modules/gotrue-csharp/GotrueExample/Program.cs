using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;

namespace GotrueExample
{
    internal static class Program
    {
        private static Random random = new();
        static void Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<IGotrueClient<User, Session>, Client>();
                    services.AddSingleton<IGotrueSessionPersistence<Session>, ClientPersistence>();
                    services.AddLogging();
                })
                .Build();

            UseClient(host.Services);

            host.Run();
        }

        static async void UseClient(IServiceProvider services)
        {
            using IServiceScope serviceScope = services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            var client = provider.GetRequiredService<IGotrueClient<User, Session>>();
            var sessionPersistence = provider.GetRequiredService<IGotrueSessionPersistence<Session>>();
            client.SetPersistence(sessionPersistence);

            Session session = null;
            var email = $"{RandomString(12)}@supabase.io";
            session = await client.SignUp(email, "iamverysecretdontguessme");

            Debug.WriteLine(client);
        }

        private static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
