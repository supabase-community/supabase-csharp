using System.Collections.Generic;

namespace Supabase.Gotrue.Mfa
{
	public class MfaListFactorsResponse
	{
		// All available factors (verified and unverified)
		public List<Factor> All { get; set; } = new List<Factor>();

		// Only verified TOTP factors. (A subset of `all`.)
		public List<Factor> Totp { get; set; } = new List<Factor>();
	}
}