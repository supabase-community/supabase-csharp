namespace Supabase.Gotrue
{
	/// <summary>
	/// A utility class that represents a successful response from a request to send a user
	/// Passwordless Sign In.
	/// </summary>
	public class PasswordlessSignInState
	{
		/// <summary>
		/// PKCE Verifier generated if using the PKCE flow type.
		/// </summary>
		public string? PKCEVerifier { get; set; }
	}
}
