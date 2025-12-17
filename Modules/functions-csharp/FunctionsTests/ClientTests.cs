using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Functions;
using static Supabase.Functions.Client;

namespace FunctionsTests
{
    [TestClass]
    public class ClientTests
    {
        private Client _client = null!;
        private string _token = null!;

        [TestInitialize]
        public void Initialize()
        {
            _token = GenerateToken("super-secret-jwt-token-with-at-least-32-characters-long");
            _client = new Client("http://localhost:54321/functions/v1");
        }

        [TestMethod("Invokes a function.")]
        public async Task Invokes()
        {
            const string function = "hello";

            var result = await _client.Invoke(
                function,
                _token,
                new InvokeFunctionOptions
                {
                    Body = new Dictionary<string, object> { { "name", "supabase" } },
                    HttpMethod = HttpMethod.Post,
                }
            );

            Assert.IsTrue(result.Contains("supabase"));

            var result2 = await _client.Invoke<Dictionary<string, string>>(
                function,
                _token,
                new InvokeFunctionOptions
                {
                    Body = new Dictionary<string, object> { { "name", "functions" } },
                    HttpMethod = HttpMethod.Post,
                }
            );

            Assert.IsInstanceOfType(result2, typeof(Dictionary<string, string>));
            Assert.IsTrue(result2.ContainsKey("message"));
            Assert.IsTrue(result2["message"].Contains("functions"));

            var result3 = await _client.RawInvoke(
                function,
                _token,
                new InvokeFunctionOptions
                {
                    Body = new Dictionary<string, object> { { "name", "functions" } },
                    HttpMethod = HttpMethod.Post,
                }
            );

            var bytes = await result3.ReadAsByteArrayAsync();

            Assert.IsInstanceOfType(bytes, typeof(byte[]));
            
            var result4 = await _client.Invoke(
                function,
                _token,
                new InvokeFunctionOptions
                {
                    Body = [],
                    HttpMethod = HttpMethod.Get,
                }
            );

            Assert.IsTrue(result4.Contains(function));
        }

        private static string GenerateToken(string secret)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                SigningCredentials = new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.HmacSha256Signature
                ),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(securityToken);
        }
    }
}