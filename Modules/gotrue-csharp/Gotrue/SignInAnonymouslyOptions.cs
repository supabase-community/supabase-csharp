using System.Collections.Generic;
using Newtonsoft.Json;
using static Supabase.Gotrue.Constants;

namespace Supabase.Gotrue
{
	/// <summary>
	/// Options for handling signing in anonymously
	/// </summary>
	public class SignInAnonymouslyOptions
	{
		/// <summary>
		///  A custom data object to store the user's metadata. This maps to the `auth.users.raw_user_meta_data` column.
		///
		/// The `data` should be a JSON serializable object that includes user-specific info, such as their first and last name.
		/// </summary>
		[JsonProperty("data")]
		public Dictionary<string, object>? Data { get; set; }

		/// <summary>
		/// Verification token received when the user completes the captcha on the site.
		/// </summary>
		[JsonProperty("captchaToken")]
		public string? CaptchaToken { get; set; }
	}
}
