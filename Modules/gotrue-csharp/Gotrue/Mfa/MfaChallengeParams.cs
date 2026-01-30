namespace Supabase.Gotrue.Mfa
{
	public class MfaChallengeParams
	{
		// Id of the factor to be challenged. Returned in enroll().
		public string FactorId { get; set; }
	}
}