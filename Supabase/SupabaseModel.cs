using System;
using System.Threading;
using System.Threading.Tasks;
using Postgrest.Models;
using Postgrest.Responses;

namespace Supabase
{
    public abstract class SupabaseModel : BaseModel
    {
        public override Task<ModeledResponse<T>> Update<T>(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Client.Instance.From<T>().Update(this as T);
        }

        public override Task Delete<T>(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Client.Instance.From<T>().Delete(this as T);
        }
    }
}
