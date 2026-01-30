using Newtonsoft.Json;

namespace Supabase.Gotrue
{
	/// <summary>
	/// Options used for signing in a user using single sign on (SSO).
	/// </summary>
	public class SignInWithSSOOptions : SignInOptions
	{
		/// <summary>
		/// Verification token received when the user completes the captcha on the site.
		/// </summary>
		[JsonProperty("captchaToken")]
		public string? CaptchaToken { get; set; }
	}
}
