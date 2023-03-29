using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SupabaseTests.Stubs
{
    internal class FakeAuthClient : IGotrueClient<User, Session>
    {
        public Session CurrentSession => throw new NotImplementedException();

        public User CurrentUser => throw new NotImplementedException();

        public Func<Dictionary<string, string>> GetHeaders { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler<ClientStateChanged> StateChanged;

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

        public Task<string> SignIn(Constants.Provider provider, string scopes = null, SignInOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<Session> SignInWithPassword(string email, string password)
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
    }
}
