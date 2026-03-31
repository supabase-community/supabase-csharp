using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Supabase.Core.Attributes;
using Supabase.Core.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Supabase.Gotrue
{
	/// <summary>
	/// Parameters for the Resend API.
	/// </summary>
	public class ResendParams
	{
		/// <summary>
		/// The type of resend.
		/// </summary>
		[JsonConverter(typeof(ResendTypeConverter))]
		public enum ResendType
		{
			/// <summary>Signup resend.</summary>
			[MapTo("signup")]
			Signup,
			/// <summary>Signup resend.</summary>
			[MapTo("signup")]
			SignUp,
			/// <summary>Invite resend.</summary>
			[MapTo("invite")]
			Invite,
			/// <summary>Magic link resend.</summary>
			[MapTo("magiclink")]
			MagicLink,
			/// <summary>Email change resend.</summary>
			[MapTo("email_change")]
			EmailChange,
			/// <summary>SMS recovery resend.</summary>
			[MapTo("recovery")]
			Recovery
		}

		/// <summary>
		/// Custom converter to handle MapTo attributes for the ResendType enum.
		/// </summary>
		public class ResendTypeConverter : JsonConverter<ResendType>
		{
			/// <inheritdoc />
			public override void WriteJson(JsonWriter writer, ResendType value, JsonSerializer serializer)
			{
				var field = value.GetType().GetField(value.ToString());
				var attribute = field?.GetCustomAttributes(typeof(MapToAttribute), false).FirstOrDefault() as MapToAttribute;
				writer.WriteValue(attribute?.Mapping ?? value.ToString().ToLower());
			}

			/// <inheritdoc />
			public override ResendType ReadJson(JsonReader reader, System.Type objectType, ResendType existingValue, bool hasExistingValue, JsonSerializer serializer)
			{
				// Implementation for reading if needed, but not strictly required for this API
				return ResendType.Signup;
			}
		}

		/// <summary>
		/// The email address to resend to.
		/// </summary>
		[JsonProperty("email")]
		public string? Email { get; set; }

		/// <summary>
		/// The phone number to resend to.
		/// </summary>
		[JsonProperty("phone")]
		public string? Phone { get; set; }

		/// <summary>
		/// The type of resend.
		/// </summary>
		[JsonProperty("type")]
		public ResendType Type { get; set; }

		/// <summary>
		/// Verification token received when the user completes the captcha on the site.
		/// </summary>
		[JsonProperty("captchaToken")]
		public string? CaptchaToken { get; set; }

		/// <summary>
		/// A URL or mobile address to send the user to after they are confirmed.
		/// </summary>
		[JsonProperty("redirectTo")]
		public string? RedirectTo { get; set; }

		/// <summary>
		/// A custom data object to store the user's metadata. This maps to the `auth.users.user_metadata` column.
		/// </summary>
		[JsonProperty("data")]
		public Dictionary<string, object>? Data { get; set; }
	}
}
