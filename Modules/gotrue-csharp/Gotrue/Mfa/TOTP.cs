using Newtonsoft.Json;

namespace Supabase.Gotrue.Mfa
{
	public class TOTP
	{
		/** Contains a QR code encoding the authenticator URI. You can
		 * convert it to a URL by prepending `data:image/svg+xml;utf-8,` to
		 * the value. Avoid logging this value to the console. */
		[JsonProperty("qr_code")]
		public string QrCode { get; set; }

		/** The TOTP secret (also encoded in the QR code). Show this secret
		 * in a password-style field to the user, in case they are unable to
		 * scan the QR code. Avoid logging this value to the console. */
		[JsonProperty("secret")]
		public string Secret { get; set; }

		/** The authenticator URI encoded within the QR code, should you need
		 * to use it. Avoid logging this value to the console. */
		[JsonProperty("uri")]
		public string Uri { get; set; }
	}
}