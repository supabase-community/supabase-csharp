using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SupabaseTests.Stubs
{
    internal class FakeAuthClient : IGotrueClient<User, Session>
    {
        public Task<Settings> Settings()
        {
            throw new NotImplementedException();
        }

        public Session CurrentSession => throw new NotImplementedException();

        public User CurrentUser => throw new NotImplementedException();
        public ClientOptions Options { get; }

        public Func<Dictionary<string, string>> GetHeaders { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void SetPersistence(IGotrueSessionPersistence<Session> persistence) {
            throw new NotImplementedException(); }
        public void AddStateChangedListener(IGotrueClient<User, Session>.AuthEventHandler authEventHandler) { }
        public void RemoveStateChangedListener(IGotrueClient<User, Session>.AuthEventHandler authEventHandler) { }
        public void ClearStateChangedListeners() { throw new NotImplementedException(); }
        public void NotifyAuthStateChange(Constants.AuthState stateChanged) { throw new NotImplementedException(); }

        public Task<User> CreateUser(string jwt, AdminUserAttributes attributes)
        {
            throw new NotImplementedException();
        }

        public Task<User> CreateUser(string jwt, string email, string password, AdminUserAttributes attributes = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteUser(string uid, string jwt)
        {
            throw new NotImplementedException();
        }

		public Task<Session> GetSessionFromUrl(Uri uri, bool storeSession = true)
        {
            throw new NotImplementedException();
        }

        public Task<User> GetUser(string jwt)
        {
            throw new NotImplementedException();
        }

        public void Debug(string message, Exception e = null)
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public Task RefreshToken()
        {
            throw new NotImplementedException();
        }

        public bool Online { get; set; }

        public Task<User> GetUserById(string jwt, string userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> InviteUserByEmail(string email, string jwt)
        {
            throw new NotImplementedException();
        }

        public Task<UserList<User>> ListUsers(string jwt, string filter = null, string sortBy = null, Constants.SortOrder sortOrder = Constants.SortOrder.Descending, int? page = null, int? perPage = null)
        {
            throw new NotImplementedException();
        }

        public Task<Session> RefreshSession()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ResetPasswordForEmail(string email)
        {
            throw new NotImplementedException();
        }

        public Task<Session> RetrieveSessionAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendMagicLink(string email, SignInOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<Session> SetSession(string accessToken, string refreshToken, bool forceAccessTokenRefresh = false)
        {
            throw new NotImplementedException();
        }

        public Session SetAuth(string accessToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> SignIn(Constants.Provider provider, string scopes = null)
        {
            throw new NotImplementedException();
        }

        public Task<Session> SignIn(Constants.SignInType type, string identifierOrToken, string password = null, string scopes = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignIn(string email, SignInOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<Session> SignIn(string email, string password)
        {
            throw new NotImplementedException();
        }

        public Task<PasswordlessSignInState> SignInWithOtp(SignInWithPasswordlessEmailOptions options) { throw new NotImplementedException(); }
        public Task<PasswordlessSignInState> SignInWithOtp(SignInWithPasswordlessPhoneOptions options) { throw new NotImplementedException(); }

        public Task<Session> SignInWithPassword(string email, string password)
        {
            throw new NotImplementedException();
        }

        public Task<ProviderAuthState> SignIn(Constants.Provider provider, SignInOptions options = null) { throw new NotImplementedException(); }
        public Task<Session> SignInWithIdToken(Constants.Provider provider, string idToken, string nonce = null, string captchaToken = null) { throw new NotImplementedException(); }
        public Task<Session> ExchangeCodeForSession(string codeVerifier, string authCode) { throw new NotImplementedException(); }

        public Task<bool> Reauthenticate()
        {
            throw new NotImplementedException();
        }

        public Task SignOut()
        {
            throw new NotImplementedException();
        }

        public Task<Session> SignUp(Constants.SignUpType type, string identifier, string password, SignUpOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<Session> SignUp(string email, string password, SignUpOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<User> Update(UserAttributes attributes)
        {
            throw new NotImplementedException();
        }

        public Task<User> UpdateUserById(string jwt, string userId, AdminUserAttributes userData)
        {
            throw new NotImplementedException();
        }

        public Task<Session> VerifyOTP(string phone, string token, Constants.MobileOtpType type = Constants.MobileOtpType.SMS)
        {
            throw new NotImplementedException();
        }

        public Task<Session> VerifyOTP(string email, string token, Constants.EmailOtpType type = Constants.EmailOtpType.MagicLink)
        {
            throw new NotImplementedException();
        }

        public void AddDebugListener(Action<string, Exception> logDebug)
        {
            throw new NotImplementedException();
        }

        public void LoadSession()
        {
            throw new NotImplementedException();
        }
    }
}
