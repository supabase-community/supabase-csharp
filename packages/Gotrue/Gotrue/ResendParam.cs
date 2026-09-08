using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Supabase.Gotrue;

/// <summary>
/// Parameters for the Resend API.
/// </summary>
public class ResendParam
{
    /// <summary>
    /// The email address to resend to.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// The phone number to resend to.
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// The type of resend.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Verification token received when the user completes the captcha on the site.
    /// </summary>
    [JsonPropertyName("captchaToken")]
    public string? CaptchaToken { get; set; }

    /// <summary>
    /// A URL or mobile address to send the user to after they are confirmed.
    /// </summary>
    [JsonPropertyName("redirectTo")]
    public string? RedirectTo { get; set; }

    /// <summary>
    /// A custom data object to store the user's metadata. This maps to the `auth.users.user_metadata` column.
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>
    /// Represents the parameters used for the Resend API, allowing for resending
    /// confirmation codes or links based on the provided inputs. This includes
    /// support for email, phone, and custom data.
    /// </summary>
    public ResendParam(Constants.ResendType type) => this.Type = Core.Helpers.GetMappedToAttr(type).Mapping;
}
