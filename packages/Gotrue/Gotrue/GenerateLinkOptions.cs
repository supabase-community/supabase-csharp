using System.Collections.Generic;
using Newtonsoft.Json;
using Supabase.Core.Attributes;

namespace Supabase.Gotrue
{
	/// <summary>
	/// Options for Generating an Email Link
	/// </summary>
	public class GenerateLinkOptions
	{
		/// <summary>
		/// Mapping of link types that can be generated.
		/// </summary>
		public enum LinkType
		{
			/// <summary>
			/// Generate a signup link.
			/// </summary>
			[MapTo("signup")]
			SignUp,
			/// <summary>
			/// Generate an invite link.
			/// </summary>
			[MapTo("invite")]
			Invite,
			/// <summary>
			/// Generate a magic link.
			/// </summary>
			[MapTo("magiclink")]
			MagicLink,
			/// <summary>
			/// Generate a recovery link.
			/// </summary>
			[MapTo("recovery")]
			Recovery,
			/// <summary>
			/// Generate an email change link to be sent to the current email address.
			/// </summary>
			[MapTo("email_change_current")]
			EmailChangeCurrent,
			/// <summary>
			/// Generate an email change link to be sent to the new email address.
			/// </summary>
			[MapTo("email_change_new")]
			EmailChangeNew
		}
		
		/// <summary>
		/// The type of link being generated
		/// </summary>
		[JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
		public string Type { get; }
		
		/// <summary>
		/// The User's Email
		/// </summary>
		[JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
		public string Email { get; }
		
		/// <summary>
		/// Only required if generating a signup link.
		/// </summary>
		[JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
		public string? Password { get; set; }
		
		/// <summary>
		/// The user's new email. Only required if type is 'email_change_current' or 'email_change_new'.
		/// </summary>
		[JsonProperty("new_email", NullValueHandling = NullValueHandling.Ignore)]
		public string? NewEmail { get; set; }
		
		/// <summary>
		/// A custom data object to store the user's metadata. This maps to the `auth.users.user_metadata` column.
		///
		/// The `data` should be a JSON encodable object that includes user-specific info, such as their first and last name.
		/// </summary>
		[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
		public Dictionary<string, object>? Data { get; set; }
		
		/// <summary>
		/// The URL which will be appended to the email link generated.
		/// </summary>
		[JsonIgnore]
		public string? RedirectTo { get; set; }

		/// <summary>
		/// Constructs options, additional properties may need to be assigned depending on <see cref="LinkType"/>
		///
		/// - <see cref="NewEmail"/> is required for <see cref="LinkType.EmailChangeCurrent"/> and <see cref="LinkType.EmailChangeNew"/>
		/// - <see cref="Password"/> is required for <see cref="LinkType.SignUp"/>
		/// - <see cref="Data"/> is optional for <see cref="LinkType.SignUp"/>
		/// </summary>
		/// <param name="linkType"></param>
		/// <param name="email"></param>
		public GenerateLinkOptions(LinkType linkType, string email)
		{
			Type = Core.Helpers.GetMappedToAttr(linkType).Mapping;
			Email = email;
		}
	}

	/// <summary>
	/// Shortcut options for <see cref="GenerateLinkOptions.LinkType.SignUp"/>
	/// </summary>
	public class GenerateLinkSignupOptions : GenerateLinkOptions
	{
		/// <summary>
		/// Constructs options for <see cref="GenerateLinkOptions.LinkType.SignUp"/>
		/// </summary>
		/// <param name="email"></param>
		/// <param name="password"></param>
		/// <remarks>
		/// <see cref="GenerateLinkOptions.Data"/> is optional
		/// </remarks>
		public GenerateLinkSignupOptions(string email, string password) : base(LinkType.SignUp, email)
		{
			Password = password;
		}
	}
	
	/// <summary>
	/// Shortcut options for <see cref="GenerateLinkOptions.LinkType.EmailChangeCurrent"/>
	/// </summary>
	public class GenerateLinkEmailChangeCurrentOptions: GenerateLinkOptions
	{
		/// <summary>
		/// Constructs options for <see cref="GenerateLinkOptions.LinkType.EmailChangeCurrent"/>
		/// </summary>
		/// <param name="email"></param>
		/// <param name="newEmail"></param>
		public GenerateLinkEmailChangeCurrentOptions(string email, string newEmail) : base(LinkType.EmailChangeCurrent, email)
		{
			NewEmail = newEmail;
		}
	}
	
	/// <summary>
	/// Shortcut options for <see cref="GenerateLinkOptions.LinkType.EmailChangeNew"/>
	/// </summary>
	public class GenerateLinkEmailChangeNewOptions: GenerateLinkOptions
	{
		/// <summary>
		/// Constructs options for <see cref="GenerateLinkOptions.LinkType.EmailChangeNew"/>
		/// </summary>
		/// <param name="email"></param>
		/// <param name="newEmail"></param>
		public GenerateLinkEmailChangeNewOptions(string email, string newEmail) : base(LinkType.EmailChangeNew, email)
		{
			NewEmail = newEmail;
		}
	}
}