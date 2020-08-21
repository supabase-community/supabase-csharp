using System;
using Newtonsoft.Json;

namespace Supabase.Auth
{
    public class AuthUser
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("aud")]
        public string Aud { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("confirmed_at")]
        public DateTimeOffset ConfirmedAt { get; set; }

        [JsonProperty("last_sign_in_at")]
        public DateTimeOffset LastSignInAt { get; set; }

        [JsonProperty("app_metadata")]
        public AppMetadata AppMetadata { get; set; }

        [JsonProperty("user_metadata")]
        public object UserMetadata { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
