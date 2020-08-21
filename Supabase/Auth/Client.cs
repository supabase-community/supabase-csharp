using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Supabase.Auth
{
    public class Client
    {
        private string authUrl;
        private ClientAuthorization authorization;
        private CancellationTokenSource persistCancellationTokenSource;
        private Dictionary<string, string> requestHeaders => new Dictionary<string, string>
        {
            {"Accept", "application/json" },
            {"apikey", authorization.ApiKey }
        };

        public ClientOptions Options;

        public AuthUser CurrentUser
        {
            get => StoredSession.User;
        }

        public LoginResponse StoredSession { get; private set; }

        public Client(string authUrl, ClientAuthorization authorization, ClientOptions options)
        {
            if (authorization.Type != ClientAuthorization.AuthorizationType.ApiKey)
            {
                throw new Exception("To use Supabase.Auth, the `authorization` param must have an API key specified");
            }

            this.authUrl = authUrl;
            this.authorization = authorization;

            Options = options;

            if (Options.HandleSessionRestore != null)
            {
                StoredSession = Options.HandleSessionRestore.Invoke();
            }
        }

        ~Client()
        {
            DestroyPersistTimer();
        }

        public async Task<AuthUser> SignUp(string email, string password)
        {
            DestroySession();

            var parameters = new Dictionary<string, string> {
                {"email", email },
                {"password", password }
            };

            try
            {
                var result = await Helpers.MakeRequest(HttpMethod.Post, $"{authUrl}/signup", parameters, requestHeaders);
                var response = JsonConvert.DeserializeObject<LoginResponse>(result);

                StoreSession(response);

                if (Options.AutoRefreshToken)
                    InitilizePersistTimer();

                return CurrentUser;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw e;
            }
        }

        public async Task<AuthUser> Login(string email, string password)
        {
            DestroySession();

            var parameters = new Dictionary<string, string> {
                {"email", email },
                {"password", password }
            };

            try
            {
                var result = await Helpers.MakeRequest(HttpMethod.Post, $"{authUrl}/token?grant_type=password", parameters, requestHeaders);
                var response = JsonConvert.DeserializeObject<LoginResponse>(result);

                StoreSession(response);

                if (Options.AutoRefreshToken)
                    InitilizePersistTimer();

                return CurrentUser;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw e;
            }
        }

        public void Logout() => DestroySession();

        public async Task<LoginResponse> RefreshToken()
        {
            var parameters = new Dictionary<string, string> {
                {"refresh_token", StoredSession.RefreshToken }
            };

            try
            {
                var result = await Helpers.MakeRequest(HttpMethod.Post, $"{authUrl}/token?grant_type=refresh_token", parameters, requestHeaders);
                var response = JsonConvert.DeserializeObject<LoginResponse>(result);

                StoreSession(response);

                return StoredSession;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw e;
            }
        }

        private void InitilizePersistTimer()
        {
            persistCancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!persistCancellationTokenSource.IsCancellationRequested)
                {

                    var span = TimeSpan.FromSeconds(StoredSession.ExpiresInSeconds - 60);

                    Debug.WriteLine($"Refreshing Auth token in {span.TotalSeconds} seconds.");

                    await Task.Delay(span, persistCancellationTokenSource.Token);
                    await RefreshToken();
                }
            }, persistCancellationTokenSource.Token);
        }

        private void DestroyPersistTimer() => persistCancellationTokenSource?.Cancel();

        private void DestroySession()
        {
            if (Options.HandleSessionDestroy != null)
            {
                var destroyResult = Options.HandleSessionDestroy.Invoke();

                if (!destroyResult)
                    throw new Exception("Failed to destroy existing auth session");
            }

            DestroyPersistTimer();

            StoredSession = null;
        }

        private void StoreSession(LoginResponse response)
        {
            if (Options.HandleSessionSave != null)
            {
                var saveResult = Options.HandleSessionSave.Invoke(response);

                if (!saveResult)
                    throw new Exception("Failed to save auth session.");
            }

            StoredSession = response;
        }
    }
}
