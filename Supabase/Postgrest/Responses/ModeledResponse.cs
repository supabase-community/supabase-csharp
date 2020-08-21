using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Supabase.Postgrest.Responses
{
    public class ModeledResponse<T> : BaseResponse
    {
        public ModeledResponse(BaseResponse baseResponse, bool shouldParse = true)
        {
            Content = baseResponse.Content;
            ResponseMessage = baseResponse.ResponseMessage;

            if (shouldParse)
            {
                Models = JsonConvert.DeserializeObject<List<T>>(Content);
            }
        }

        public List<T> Models { get; set; } = new List<T>();
    }
}
