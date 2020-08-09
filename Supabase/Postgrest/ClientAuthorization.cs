using System;
namespace Supabase.Postgrest
{
    public class ClientAuthorization
    {
        public string ApiKey { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public ClientAuthorization(string apiKey)
        {
            ApiKey = apiKey;
        }

        public ClientAuthorization(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
