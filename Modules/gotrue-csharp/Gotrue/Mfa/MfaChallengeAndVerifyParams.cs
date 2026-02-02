namespace Supabase.Gotrue.Mfa
{
	public class MfaChallengeAndVerifyParams
	{
		public string FactorId { get; set; }
		public string Code { get; set; }
	}
}