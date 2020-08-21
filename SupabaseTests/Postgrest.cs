using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase;
using Supabase.Postgrest;
using SupabaseTests.Models;
using static Supabase.ClientAuthorization;

namespace SupabaseTests
{
    [TestClass]
    public class Postgrest
    {
        private static string baseUrl = "http://localhost:3000";

        [TestMethod("Initilizes")]
        public void TestInitilization()
        {
            var client = new Client<User>(baseUrl, null, null);
            Assert.AreEqual(client.BaseUrl, baseUrl);
        }

        [TestMethod("with optional query params")]
        public void TestQueryParams()
        {
            Client<User> client = new Client<User>(baseUrl, null, options: new ClientOptions
            {
                QueryParams = new Dictionary<string, string>
                {
                    { "some-param", "foo" },
                    { "other-param", "bar" }
                }
            });

            Assert.AreEqual(client.GenerateUrl(), $"{baseUrl}/?some-param=foo&other-param=bar");
        }

        [TestMethod("will use TableAttribute")]
        public void TestTableAttribute()
        {
            var client = new Client<User>(baseUrl, null);
            Assert.AreEqual(client.GenerateUrl(), $"{baseUrl}/users");
        }

        [TestMethod("will default to Class.name in absence of TableAttribute")]
        public void TestTableAttributeDefault()
        {
            var client = new Client<Stub>(baseUrl, null);
            Assert.AreEqual(client.GenerateUrl(), $"{baseUrl}/Stub");
        }

        [TestMethod("will set Authorization header from token")]
        public void TestHeadersToken()
        {
            var client = new Client<User>(baseUrl, new Supabase.ClientAuthorization(AuthorizationType.Token, "token"), null);
            var headers = client.PrepareRequestHeaders();

            Assert.AreEqual(headers["Authorization"], "Bearer token");
        }

        [TestMethod("will set apikey query string")]
        public void TestQueryApiKey()
        {
            var client = new Client<User>(baseUrl, new ClientAuthorization(AuthorizationType.ApiKey, "some-key"));
            Assert.AreEqual(client.GenerateUrl(), $"{baseUrl}/?apikey=some-key");
        }

        [TestMethod("will set Basic Authorization")]
        public void TestHeadersBasicAuth()
        {
            var user = "user";
            var pass = "pass";
            var client = new Client<User>(baseUrl, new ClientAuthorization(user, pass), null);
            var headers = client.PrepareRequestHeaders();
            var expected = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user}:{pass}"));

            Assert.AreEqual(headers["Authorization"], $"Basic {expected}");
        }

        // TODO: Flesh out remaining tests
    }
}
