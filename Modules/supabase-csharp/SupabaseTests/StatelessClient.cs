using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using SupabaseTests.Models;
using static Supabase.Gotrue.Constants;
using static Supabase.StatelessClient;

namespace SupabaseTests
{
    [TestClass]
    public class StatelessClient
    {

        private string supabaseUrl = "http://localhost";
        private Supabase.SupabaseOptions options = new()
        {
            AuthUrlFormat = "{0}:54321/rest/v1",
            RealtimeUrlFormat = "ws://127.0.0.1:54321/realtime/v1",
            RestUrlFormat = "{0}:54321/rest/v1",
        };

        [TestMethod("Can access Stateless REST")]
        public async Task CanAccessStatelessRest()
        {
            var restOptions = GetRestOptions("sb_secret_N7UND0UgjKTVK-Uodkm0Hg_xSvEMPvz", options);
            var result1 = await new Supabase.Postgrest.Client(String.Format(options.RestUrlFormat, supabaseUrl), restOptions).Table<Channel>().Get();

            var result2 = await From<Channel>(supabaseUrl, "sb_secret_N7UND0UgjKTVK-Uodkm0Hg_xSvEMPvz", options).Get();

            Assert.AreEqual(result1.Models.Count, result2.Models.Count);
        }

        [TestMethod("Can access Stateless GoTrue")]
        public void CanAccessStatelessGotrue()
        {
            var gotrueOptions = GetAuthOptions(supabaseUrl, null, options);

            var client = new Supabase.Gotrue.Client(gotrueOptions);

            var url = client.SignIn(Provider.Spotify);

            Assert.IsNotNull(url);
        }

        [TestMethod("User defined Headers will override internal headers")]
        public void CanOverrideInternalHeaders()
        {
            Supabase.SupabaseOptions options = new Supabase.SupabaseOptions
            {
                AuthUrlFormat = "{0}:9999",
                RealtimeUrlFormat = "{0}:4000/socket",
                RestUrlFormat = "{0}:3000",
                Headers = new Dictionary<string, string> {
                    { "Authorization", "Bearer 123" }
                }
            };

            var gotrueOptions = GetAuthOptions(supabaseUrl, "456", options);

            Assert.AreEqual("Bearer 123", gotrueOptions.Headers["Authorization"]);
        }
    }
}
