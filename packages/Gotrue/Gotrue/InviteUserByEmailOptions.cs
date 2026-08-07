using System.Collections.Generic;

namespace Supabase.Gotrue
{
	/// <summary>
	/// A utility class that represents options for sending a User an Invitation
	/// </summary>
	public class InviteUserByEmailOptions
	{
		/// <summary>
		/// The URL which will be appended to the email link sent to the user's email address. Once clicked the user will end up on this URL.
		/// </summary>
		public string? RedirectTo { get; set; }

		/// <summary>
		/// A custom data object to store additional metadata about the user. This maps to the `auth.users.user_metadata` column.
		/// </summary>
		public Dictionary<string, object>? Data { get; set; }
	}
}