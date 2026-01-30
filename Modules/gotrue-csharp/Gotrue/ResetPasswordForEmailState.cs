namespace Supabase.Gotrue
{
	/// <summary>
	/// A utility class that represents a successful response from a request to send a user's password reset using the PKCE flow.
	/// </summary>
	public class ResetPasswordForEmailState
	{
		/// <summary>
		/// PKCE Verifier generated if using the PKCE flow type.
		/// </summary>
		public string? PKCEVerifier { get; set; }
	}
}