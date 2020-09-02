using System;
using static Postgrest.ClientAuthorization;

namespace Supabase.Extensions
{
    public static class ClientAuthorizationExtensions
    {

        public static Postgrest.ClientAuthorization ToPostgrestAuth(this ClientAuthorization authorization)
        {
            switch(authorization.Type)
            {
                case ClientAuthorization.AuthorizationType.Token:
                    return new Postgrest.ClientAuthorization(AuthorizationType.Token, authorization.Token);
                case ClientAuthorization.AuthorizationType.ApiKey:
                    return new Postgrest.ClientAuthorization(AuthorizationType.ApiKey, authorization.ApiKey);
                case ClientAuthorization.AuthorizationType.Basic:
                    return new Postgrest.ClientAuthorization(AuthorizationType.Basic, authorization.Username, authorization.Password);
                default:
                    return new Postgrest.ClientAuthorization(AuthorizationType.Open, null);
            }
        }
    }
}
