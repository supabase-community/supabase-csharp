namespace Supabase.Gotrue.Mfa
{
	public class MfaAdminDeleteFactorParams
	{
		// Id of the MFA factor to delete
		public string Id { get; set; }

		// Id of the user whose factor is being deleted
		public string UserId { get; set; }
	}
}