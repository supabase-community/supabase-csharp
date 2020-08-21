using System;
namespace Supabase
{
    public class ClientAuthorization
    {
        public string ApiKey { get; set; }
        public string Token { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public AuthorizationType Type { get; set; }

        public enum AuthorizationType
        {
            Open,
            ApiKey,
            Token,
            Basic
        }

        public ClientAuthorization()
        {
            Type = AuthorizationType.Open;
        }

        public ClientAuthorization(string apiKey)
        {
            Type = AuthorizationType.ApiKey;
            ApiKey = apiKey;
        }

        public ClientAuthorization(string username, string password)
        {
            Type = AuthorizationType.Basic;
            Username = username;
            Password = password;
        }

        public ClientAuthorization(AuthorizationType type, string value1, string value2 = null)
        {
            Type = type;
            switch (type)
            {
                case AuthorizationType.ApiKey:
                    ApiKey = value1;
                    break;
                case AuthorizationType.Basic:
                    Username = value1;
                    Password = value2;
                    break;
                case AuthorizationType.Token:
                    Token = value1;
                    break;
            }
        }
    }
}
