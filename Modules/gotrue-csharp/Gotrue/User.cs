#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Supabase.Gotrue.Mfa;

#pragma warning disable CS1591

namespace Supabase.Gotrue
{
	/// <summary>
	/// Represents a Gotrue User
	/// Ref: https://supabase.github.io/gotrue-js/interfaces/User.html
	/// </summary>
	public class User
	{
		[JsonProperty("app_metadata")]
		public Dictionary<string, object> AppMetadata { get; set; } = new Dictionary<string, object>();

		[JsonProperty("aud")]
		public string? Aud { get; set; }

		[JsonProperty("confirmation_sent_at")]
		public DateTime? ConfirmationSentAt { get; set; }

		[JsonProperty("confirmed_at")]
		public DateTime? ConfirmedAt { get; set; }

		[JsonProperty("created_at")]
		public DateTime CreatedAt { get; set; }

		[JsonProperty("email")]
		public string? Email { get; set; }

		[JsonProperty("email_confirmed_at")]
		public DateTime? EmailConfirmedAt { get; set; }

		[JsonProperty("id")]
		public string? Id { get; set; }

		[JsonProperty("identities")]
		public List<UserIdentity> Identities { get; set; } = new List<UserIdentity>();

		[JsonProperty("invited_at")]
		public DateTime? InvitedAt { get; set; }

		[JsonProperty("last_sign_in_at")]
		public DateTime? LastSignInAt { get; set; }

		[JsonProperty("phone")]
		public string? Phone { get; set; }

		[JsonProperty("phone_confirmed_at")]
		public DateTime? PhoneConfirmedAt { get; set; }

		[JsonProperty("recovery_sent_at")]
		public DateTime? RecoverySentAt { get; set; }

		[JsonProperty("role")]
		public string? Role { get; set; }

		[JsonProperty("updated_at")]
		public DateTime? UpdatedAt { get; set; }

		[JsonProperty("banned_until")]
		public DateTime? BannedUntil { get; set; }

		[JsonProperty("is_anonymous")]
		public bool IsAnonymous { get; set; }

		[JsonProperty("factors")]
		public List<Factor> Factors { get; set; } = new List<Factor>();

		[JsonProperty("user_metadata")]
		public Dictionary<string, object> UserMetadata { get; set; } = new Dictionary<string, object>();
	}

	/// <summary>
	/// Ref: https://supabase.github.io/gotrue-js/interfaces/AdminUserAttributes.html
	/// </summary>
	public class AdminUserAttributes : UserAttributes
	{
		/// <summary>
		/// A custom data object for app_metadata that. Can be any JSON serializable data.
		/// Only a service role can modify
		///
		/// Note: GoTrue does not yest support creating a user with app metadata
		///     (see: https://github.com/supabase/gotrue-js/blob/d7b334a4283027c65814aa81715ffead262f0bfa/test/GoTrueApi.test.ts#L45)
		/// </summary>
		[JsonProperty("app_metadata")]
		public Dictionary<string, object> AppMetadata { get; set; } = new Dictionary<string, object>();

		/// <summary>
		/// A custom data object for user_metadata. Can be any JSON serializable data.
		/// Only a service role can modify.
		/// </summary>
		[JsonProperty("user_metadata")]
		public Dictionary<string, object> UserMetadata { get; set; } = new Dictionary<string, object>();

		/// <summary>
		/// Sets if a user has confirmed their email address.
		/// Only a service role can modify
		/// </summary>
		[JsonProperty("email_confirm")]
		public bool? EmailConfirm { get; set; }

		/// <summary>
		/// Sets if a user has confirmed their phone number.
		/// Only a service role can modify
		/// </summary>
		[JsonProperty("phone_confirm")]
		public bool? PhoneConfirm { get; set; }

		/// <summary>
		/// <para>Determines how long a user is banned for. </para>
		/// <para>This property is ignored when creating a user.
		/// If you want to create a user banned, first create the user then update it sending this property.</para>
		/// <para>The format for the ban duration follows a strict sequence of decimal numbers with a unit suffix.
		/// Valid time units are "ns", "us" (or "µs"), "ms", "s", "m", "h".</para>
		/// <para>For example, some possible durations include: '300ms', '2h45m', '1200s'.</para>
		/// <para>Setting the ban duration to "none" lifts the ban on the user.</para>
		/// <para>Only a service role can modify.</para>
		/// </summary>
		[JsonProperty("ban_duration")]
		public string? BanDuration { get; set; }
	}

	/// <summary>
	/// Ref: https://supabase.github.io/gotrue-js/interfaces/UserAttributes.html
	/// </summary>
	public class UserAttributes
	{
		[JsonProperty("email")]
		public string? Email { get; set; }

		[JsonProperty("email_change_token")]
		public string? EmailChangeToken { get; set; }

		[JsonProperty("nonce")]
		public string? Nonce { get; set; }

		[JsonProperty("password")]
		public string? Password { get; set; }

		[JsonProperty("phone")]
		public string? Phone { get; set; }

		/// <summary>
		/// A custom data object for user_metadata that a user can modify.Can be any JSON.
		/// </summary>
		[JsonProperty("data")]
		public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
	}

	/// <summary>
	/// Ref: https://supabase.github.io/gotrue-js/interfaces/VerifyEmailOTPParams.html
	/// </summary>
	public class VerifyOTPParams
	{
		[JsonProperty("email")]
		public string? Email { get; set; }

		[JsonProperty("phone")]
		public string? Phone { get; set; }

		[JsonProperty("token")]
		public string? Token { get; set; }

		[JsonProperty("type")]
		public string? Type { get; set; }
	}

	public class UserList<TUser>
		where TUser : User
	{
		[JsonProperty("aud")]
		public string? Aud { get; set; }

		[JsonProperty("users")]
		public List<TUser> Users { get; set; } = new List<TUser>();
	}

	/// <summary>
	/// Ref: https://supabase.github.io/gotrue-js/interfaces/UserIdentity.html
	/// </summary>
	public class UserIdentity
	{
		[JsonProperty("id")]
		public string? Id { get; set; }

		[JsonProperty("user_id")]
		public string? UserId { get; set; }

		[JsonProperty("identity_data")]
		public Dictionary<string, object> IdentityData { get; set; } = new Dictionary<string, object>();

		[JsonProperty("identity_id")]
		public string IdentityId { get; set; } = null!;
		
		[JsonProperty("provider")]
		public string? Provider { get; set; }

		[JsonProperty("created_at")]
		public DateTime CreatedAt { get; set; }
		
		[JsonProperty("last_sign_in_at")]
		public DateTime LastSignInAt { get; set; }
		
		[JsonProperty("updated_at")]
		public DateTime? UpdatedAt { get; set; }
	}
}