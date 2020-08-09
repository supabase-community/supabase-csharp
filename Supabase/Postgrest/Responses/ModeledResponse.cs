using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Supabase.Postgrest.Responses
{
    public class ModeledResponse<T> : BaseResponse
    {
        public ModeledResponse(BaseResponse baseResponse)
        {
            Content = baseResponse.Content;
            ResponseMessage = baseResponse.ResponseMessage;
            Models = JsonConvert.DeserializeObject<List<T>>(Content);
        }

        public List<T> Models { get; set; } = new List<T>();
    }
}
