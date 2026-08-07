namespace Supabase.Gotrue.Mfa
{
	public class MfaEnrollParams
	{
		public string FactorType { get; set; }
		public string? Issuer { get; set; }
		public string? FriendlyName { get; set; }
	}
}