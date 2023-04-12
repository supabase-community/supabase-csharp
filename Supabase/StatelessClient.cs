using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Postgrest;
using Postgrest.Models;
using Postgrest.Responses;
using Supabase.Core;
using Supabase.Extensions;
using Supabase.Functions.Interfaces;
using Supabase.Gotrue;
using Supabase.Storage;
using Supabase.Storage.Interfaces;

namespace Supabase
{
    /// <summary>
    /// A Static class representing a Supabase Client.
    /// </summary>
    public static class StatelessClient
    {
        public static Gotrue.ClientOptions<TSession> GetAuthOptions<TSession>(string supabaseUrl, string? supabaseKey = null, SupabaseOptions? options = null)
            where TSession : Session
        {
            if (options == null)
                options = new SupabaseOptions();

            var headers = GetAuthHeaders(supabaseKey, options).MergeLeft(options.Headers);

            return new Gotrue.ClientOptions<TSession>
            {
                Url = string.Format(options.AuthUrlFormat, supabaseUrl),
                Headers = headers
            };
        }

        public static Postgrest.ClientOptions GetRestOptions(string? supabaseKey = null, SupabaseOptions? options = null)
        {
            if (options == null)
                options = new SupabaseOptions();

            var headers = GetAuthHeaders(supabaseKey, options).MergeLeft(options.Headers);

            return new Postgrest.ClientOptions
            {
                Schema = options.Schema,
                Headers = headers
            };
        }

        /// <summary>
        /// Supabase Storage allows you to manage user-generated content, such as photos or videos.
        /// </summary>
        /// <param name="supabaseUrl"></param>
        /// <param name="supabaseKey"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IStorageClient<Bucket, FileObject> Storage(string supabaseUrl, string? supabaseKey = null, SupabaseOptions? options = null)
        {
            if (options == null)
                options = new SupabaseOptions();

            var headers = GetAuthHeaders(supabaseKey, options).MergeLeft(options.Headers);

            return new Storage.Client(string.Format(options.StorageUrlFormat, supabaseUrl), headers);
        }

        /// <summary>
        /// Supabase Edge functions allow you to deploy and invoke edge functions.
        /// </summary>
        /// <param name="supabaseUrl"></param>
        /// <param name="supabaseKey"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IFunctionsClient Functions(string supabaseUrl, string supabaseKey, SupabaseOptions? options = null)
        {
            if (options == null)
                options = new SupabaseOptions();

            // See: https://github.com/supabase/supabase-js/blob/09065a65f171bc28a9fd7b831af2c24e5f1a380b/src/SupabaseClient.ts#L77-L83
            var isPlatform = new Regex(@"/(supabase\.co)|(supabase\.in)/").Match(supabaseUrl);

            string functionsUrl;
            if (isPlatform.Success)
            {
                var parts = supabaseUrl.Split('.');
                functionsUrl = $"{parts[0]}.functions.{parts[1]}.{parts[2]}";
            }
            else
            {
                functionsUrl = string.Format(options.FunctionsUrlFormat, supabaseUrl);
            }

            var headers = GetAuthHeaders(supabaseKey, options).MergeLeft(options.Headers);
            var client = new Functions.Client(functionsUrl);
            client.GetHeaders = () => headers;

            return client;
        }

        /// <summary>
        /// Gets the Postgrest client to prepare for a query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static SupabaseTable<T> From<T>(string supabaseUrl, string supabaseKey, SupabaseOptions? options = null) where T : BaseModel, new()
        {
            if (options == null)
                options = new SupabaseOptions();

            var restUrl = string.Format(options.RestUrlFormat, supabaseUrl);
            var realtimeUrl = string.Format(options.RealtimeUrlFormat, supabaseUrl).Replace("http", "ws");

            var restOptions = GetRestOptions(supabaseKey, options);
            restOptions.Headers.MergeLeft(options.Headers);

            var realtimeOptions = new Realtime.ClientOptions { Parameters = { ApiKey = supabaseKey } };

            var postgrestClient = new Postgrest.Client(restUrl, restOptions);
            var realtimeClient = new Realtime.Client(realtimeUrl, realtimeOptions);

            return new SupabaseTable<T>(postgrestClient, realtimeClient, options.Schema);
        }

        /// <summary>
        /// Runs a remote procedure.
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Task<BaseResponse> Rpc(string supabaseUrl, string supabaseKey, string procedureName, Dictionary<string, object> parameters, SupabaseOptions? options = null)
        {
            if (options == null)
                options = new SupabaseOptions();

            return new Postgrest.Client(string.Format(options.RestUrlFormat, supabaseUrl), GetRestOptions(supabaseKey, options)).Rpc(procedureName, parameters);
        }


        internal static Dictionary<string, string> GetAuthHeaders(string? supabaseKey, SupabaseOptions options)
        {
            var headers = new Dictionary<string, string>();

            headers["X-Client-Info"] = Util.GetAssemblyVersion(typeof(Client));

            if (supabaseKey != null)
            {
                headers["apiKey"] = supabaseKey;
            }

            // In Regard To: https://github.com/supabase/supabase-csharp/issues/5
            if (options.Headers.ContainsKey("Authorization"))
            {
                headers["Authorization"] = options.Headers["Authorization"];
            }
            else
            {
                headers["Authorization"] = $"Bearer {supabaseKey}";
            }

            return headers;
        }
    }
}
