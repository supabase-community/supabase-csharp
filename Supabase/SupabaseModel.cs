using System;
using System.Threading.Tasks;
using Postgrest.Models;
using Postgrest.Responses;

namespace Supabase
{
    public abstract class SupabaseModel : BaseModel
    {
        public override Task<ModeledResponse<T>> Update<T>()
        {
            return Client.Instance.From<T>().Update(this as T);
        }

        public override Task Delete<T>()
        {
            return Client.Instance.From<T>().Delete(this as T);
        }
    }
}
