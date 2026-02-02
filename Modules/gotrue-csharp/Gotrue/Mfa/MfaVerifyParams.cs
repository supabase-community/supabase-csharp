namespace Supabase.Gotrue.Mfa
{
	public class MfaVerifyParams
	{
		// ID of the factor being verified. Returned in enroll()
		public string FactorId { get; set; }

		// ID of the challenge being verified. Returned in challenge()
		public string ChallengeId { get; set; }

		// Verification code provided by the user
		public string Code { get; set; }
	}
}