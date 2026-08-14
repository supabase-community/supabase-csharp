using System.Text.Json.Serialization;

namespace Supabase.Gotrue.Responses;

/// <summary>
/// An extended user response returned when generating a link.
/// </summary>
public class GenerateLinkResponse : User
{
    /// <summary>
    /// The email link to send to the user.
    /// The action_link follows the following format: auth/v1/verify?type={verification_type}&amp;token={hashed_token}&amp;redirect_to={redirect_to}
    /// </summary>
    [JsonPropertyName("action_link")]
    public string? ActionLink { get; set; }

    /// <summary>
    /// The raw email OTP.
    /// You should send this in the email if you want your users to verify using an OTP instead of the action link.
    /// </summary>
    [JsonPropertyName("email_otp")]
    public string? EmailOtp { get; set; }

    /// <summary>
    /// The hashed token appended to the action link.
    /// </summary>
    [JsonPropertyName("hashed_token")]
    public string? HashedToken { get; set; }

    /// <summary>
    /// The URL appended to the action link.
    /// </summary>
    [JsonPropertyName("redirect_to")]
    public string? RedirectTo { get; set; }

    /// <summary>
    /// The verification type that the email link is associated to. 
    /// </summary>
    [JsonPropertyName("verification_type")]
    public string? VerificationType { get; set; }
}
