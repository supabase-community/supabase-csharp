using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Supabase.Postgrest.Responses;

namespace Supabase.Models
{
    public abstract class BaseModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("inserted_at")]
        public DateTime InsertedAt { get; set; } = new DateTime();

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; } = new DateTime();


        public virtual Task<ModeledResponse<T>> Update<T>() where T : BaseModel, new() => Client.Instance.Postgrest<T>().Update((T)this);
        public virtual Task Delete<T>() where T : BaseModel, new() => Client.Instance.Postgrest<T>().Delete((T)this);
    }
}
