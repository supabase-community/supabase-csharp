using System;

namespace Supabase.Gotrue
{
	/// <summary>
	/// Single sign on (SSO) response data deserialized from the API {supabaseAuthUrl}/sso
	/// </summary>
	public class SSOResponse : ProviderAuthState
	{
		/// <summary>
		/// Deserialized response from {supabaseAuthUrl}/sso
		/// </summary>
		/// <param name="uri">Uri from the response, this will open the SSO providers login page and allow a user to login to their provider</param>
		public SSOResponse(Uri uri) : base(uri) { }
	}
}
