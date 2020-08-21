using System;
using System.Collections.Generic;

namespace Supabase.Realtime
{
    public class ClientOptions
    {
        public string Schema { get; set; }
        public ClientAuthorization Authorization { get; set; }

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> QueryParams { get; set; } = new Dictionary<string, string>();
    }
}
