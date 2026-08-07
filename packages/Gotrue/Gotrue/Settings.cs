using System.Collections.Generic;
using Newtonsoft.Json;
#pragma warning disable CS1591

namespace Supabase.Gotrue
{
	/// <summary>
	/// Settings data retrieved from the GoTrue server.
	/// </summary>
	public class Settings
	{
		[JsonProperty("disable_signup")]
		public bool? DisableSignup { get; set; }

		[JsonProperty("mailer_autoconfirm")]
		public bool? MailerAutoConfirm { get; set; }

		[JsonProperty("phone_autoconfirm")]
		public bool? PhoneAutoConfirm { get; set; }

		[JsonProperty("sms_provider")]
		public string? SmsProvider { get; set; }

		[JsonProperty("mfa_enabled")]
		public bool? MFAEnabled { get; set; }

		// SAML = SSO enabled
		[JsonProperty("saml_enabled")]
		public bool? SAMLEnabled { get; set; }

		[JsonProperty("external")]
		public Dictionary<string, bool>? ExternalProviders { get; set; }
	}
}
