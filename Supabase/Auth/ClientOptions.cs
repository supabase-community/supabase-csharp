using System;
namespace Supabase.Auth
{
    public class ClientOptions
    {
        public bool AutoRefreshToken { get; set; } = true;

        public Func<LoginResponse, bool> HandleSessionSave;
        public Func<LoginResponse> HandleSessionRestore;
        public Func<bool> HandleSessionDestroy;
    }
}
