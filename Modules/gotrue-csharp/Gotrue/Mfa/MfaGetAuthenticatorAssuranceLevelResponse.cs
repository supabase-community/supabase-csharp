namespace Supabase.Gotrue.Mfa
{
	public class MfaGetAuthenticatorAssuranceLevelResponse
	{
		public AuthenticatorAssuranceLevel? CurrentLevel { get; set; }
		public AuthenticatorAssuranceLevel? NextLevel { get; set; }
		public AmrEntry[] CurrentAuthenticationMethods { get; set; }
	}
}