using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;
using Supabase.Postgrest.Options;

namespace SupabaseTests
{
    [TestClass]
    public class Postgrest
    {
        private static string baseUrl = "https://localhost:3000";

        [TestMethod("Initilizes")]
        public void TestInitilization()
        {
            Client client = new Client(baseUrl, null);
            Assert.AreEqual(client.BaseUrl, baseUrl);
        }

        [TestMethod("with optional query params")]
        public void TestQueryParams()
        {
            Client client = new Client(baseUrl, options: new ClientOptions
            {
                QueryParams = new Dictionary<string, string>
                {
                    { "some-param", "foo" },
                    { "other-param", "bar" }
                }
            });

            Assert.AreEqual(client.GenerateUrl(), $"{baseUrl}/?some-param=foo&other-param=bar");
        }

        [TestMethod("from(some_table)")]
        public void TestFrom()
        {
            Client client = new Client(baseUrl);
            client.From("some_table");
            Assert.AreEqual(client.GenerateUrl(), $"{baseUrl}/some_table");
        }

        [TestMethod("will set Authorization header from token")]
        public void TestHeadersToken()
        {
            Client client = new Client(baseUrl, "token", null);
            var headers = client.PrepareRequestHeaders();

            Assert.AreEqual(headers["Authorization"], "Bearer token");
        }

        [TestMethod("will set Basic Authorization")]
        public void TestHeadersBasicAuth()
        {
            var user = "user";
            var pass = "pass";
            Client client = new Client(baseUrl, new ClientAuthorization(user, pass), null);
            var headers = client.PrepareRequestHeaders();
            var expected = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user}:{pass}"));

            Assert.AreEqual(headers["Authorization"], $"Basic {expected}");
        }

        // TODO: Flesh out remaining tests
    }
}
