using static Supabase.Gotrue.Exceptions.FailureHint.Reason;

namespace Supabase.Gotrue.Exceptions
{
	/// <summary>
	/// Maps Supabase server errors to hints based on the status code and the contents of the error message.
	/// </summary>
	public static class FailureHint
	{
		/// <summary>
		/// Best effort guess at why the exception was thrown.
		/// </summary>
		public enum Reason
		{
			/// <summary>
			/// The reason for the error could not be determined.
			/// </summary>
			Unknown,
			/// <summary>
			/// The client is set to run offline or the network is unavailable.
			/// </summary>
			Offline,
			/// <summary>
			/// The user's email address has not been confirmed.
			/// </summary>
			UserEmailNotConfirmed,
			/// <summary>
			/// The user's email address and password are invalid.
			/// </summary>
			UserBadMultiple,
			/// <summary>
			/// The user's password is invalid.
			/// </summary>
			UserBadPassword,
			/// <summary>
			/// The user's login is invalid.
			/// </summary>
			UserBadLogin,
			/// <summary>
			/// The user's email address is invalid.
			/// </summary>
			UserBadEmailAddress,
			/// <summary>
			/// The user's phone number is invalid.
			/// </summary>
			UserBadPhoneNumber,
			/// <summary>
			/// The user's information is incomplete.
			/// </summary>
			UserMissingInformation,
			/// <summary>
			/// The user is already registered.
			/// </summary>
			UserAlreadyRegistered,
			/// <summary>
			/// Server rejected due to number of requests
			/// </summary>
			UserTooManyRequests,
			/// <summary>
			/// The refresh token is invalid.
			/// </summary>
			InvalidRefreshToken,
			/// <summary>
			/// The refresh token expired.
			/// </summary>
			ExpiredRefreshToken,
			/// <summary>
			/// This operation requires a bearer/service key (do not include this key in a client app)
			/// </summary>
			AdminTokenRequired,
			/// <summary>
			/// No/invalid session found
			/// </summary>
			NoSessionFound,
			/// <summary>
			/// Something wrong with the URL to session transformation
			/// </summary>
			BadSessionUrl,
			/// <summary>
			/// An invalid authentication flow has been selected.
			/// </summary>
			InvalidFlowType,
			/// <summary>
			/// The SSO domain provided was not registered via the CLI
			/// </summary>
			SsoDomainNotFound,
			/// <summary>
			/// The sso provider ID was incorrect or does not exist
			/// </summary>
			SsoProviderNotFound,
			MfaChallengeUnverified,
		}

		/// <summary>
		/// Detects the reason for the error based on the status code and the contents of the error message.
		/// </summary>
		/// <param name="gte"></param>
		/// <returns></returns>
		public static Reason DetectReason(GotrueException gte)
		{
			if (gte.Content == null)
				return Unknown;

			return gte.StatusCode switch
			{
				400 when gte.Content.Contains("Invalid login") => UserBadLogin,
				400 when gte.Content.Contains("Email not confirmed") => UserEmailNotConfirmed,
				400 when gte.Content.Contains("Invalid Refresh Token") => InvalidRefreshToken,
				400 when gte.Content.Contains("Refresh token is not valid") => InvalidRefreshToken,
				400 when gte.Content.Contains("Phone") => UserBadPhoneNumber,
				400 when gte.Content.Contains("phone") => UserBadPhoneNumber,
				400 when gte.Content.Contains("Phone") => UserBadPhoneNumber,
				400 when gte.Content.Contains("phone") => UserBadPhoneNumber,
				400 when gte.Content.Contains("Email") => UserBadEmailAddress,
				400 when gte.Content.Contains("email") => UserBadEmailAddress,
				400 when gte.Content.Contains("provide") => UserMissingInformation,
				401 when gte.Content.Contains("This endpoint requires a Bearer token") => AdminTokenRequired,
				403 when gte.Content.Contains("Invalid token") => AdminTokenRequired,
				403 when gte.Content.Contains("invalid JWT") => AdminTokenRequired,
				404 when gte.Content.Contains("No SSO provider assigned for this domain") => SsoDomainNotFound,
				404 when gte.Content.Contains("No such SSO provider") => SsoProviderNotFound,
				422 when gte.Content.Contains("User already registered") => UserAlreadyRegistered,
				422 when gte.Content.Contains("Phone") && gte.Content.Contains("Email") => UserBadMultiple,
				422 when gte.Content.Contains("email") && gte.Content.Contains("password") => UserBadMultiple,
				422 when gte.Content.Contains("Password") => UserBadPassword,
				422 when gte.Content.Contains("password") => UserBadPassword,
				429 => UserTooManyRequests,
				_ => Unknown
			};

		}
	}
}
