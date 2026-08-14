using System.Collections.Generic;
using System.Text.Json.Serialization;
#pragma warning disable CS1591

namespace Supabase.Gotrue;

/// <summary>
/// Settings data retrieved from the GoTrue server.
/// </summary>
public class Settings
{
    [JsonPropertyName("disable_signup")]
    public bool? DisableSignup { get; set; }

    [JsonPropertyName("mailer_autoconfirm")]
    public bool? MailerAutoConfirm { get; set; }

    [JsonPropertyName("phone_autoconfirm")]
    public bool? PhoneAutoConfirm { get; set; }

    [JsonPropertyName("sms_provider")]
    public string? SmsProvider { get; set; }

    [JsonPropertyName("mfa_enabled")]
    public bool? MFAEnabled { get; set; }

    // SAML = SSO enabled
    [JsonPropertyName("saml_enabled")]
    public bool? SAMLEnabled { get; set; }

    [JsonPropertyName("external")]
    public Dictionary<string, bool>? ExternalProviders { get; set; }
}
