using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Supabase.Models;
using Supabase.Postgrest.Options;
using Supabase.Postgrest.Responses;
using static Supabase.Postgrest.Helpers;

namespace Supabase.Postgrest
{
    public class Client
    {
        public string BaseUrl { get; private set; }

        private ClientAuthorization authorization;
        private ClientOptions options;

        private HttpMethod method;

        private string tableName;
        private string columnQuery;

        private List<QueryFilter> filters = new List<QueryFilter>();
        private List<QueryOrderer> orderers = new List<QueryOrderer>();

        private int rangeFrom = int.MinValue;
        private int rangeTo = int.MinValue;

        private int limit = int.MinValue;
        private string limitForeignKey;

        private int offset = int.MinValue;
        private string offsetForeignKey;


        public Client(string baseUrl, string apiKey, ClientOptions options) : this(baseUrl, new ClientAuthorization(apiKey), options) { }
        public Client(string baseUrl, ClientAuthorization authorization = null, ClientOptions options = null)
        {
            BaseUrl = baseUrl;
            this.authorization = authorization;

            if (options == null)
                options = new ClientOptions();

            this.options = options;
        }


        public Client From(string tableName)
        {
            this.tableName = tableName;
            return this;
        }

        public Client Filter(string columnName, Operator op, string criteria)
        {
            filters.Add(new QueryFilter(columnName, op, criteria));
            return this;
        }

        public Client Match(Dictionary<string, string> query)
        {
            return this;
        }

        public Client Order(string column, Ordering ordering, NullPosition nullPosition = NullPosition.First)
        {
            orderers.Add(new QueryOrderer(null, column, ordering, nullPosition));
            return this;
        }

        public Client Order(string foreignTable, string column, Ordering ordering, NullPosition nullPosition = NullPosition.First)
        {
            orderers.Add(new QueryOrderer(foreignTable, column, ordering, nullPosition));
            return this;
        }

        public Client Range(int from)
        {
            rangeFrom = from;
            return this;
        }

        public Client Range(int from, int to)
        {
            rangeFrom = from;
            rangeTo = to;
            return this;
        }

        public Client Select(string columnQuery)
        {
            method = HttpMethod.Get;
            this.columnQuery = columnQuery;
            return this;
        }

        public Client Limit(int limit, string foreignTableName = null)
        {
            this.limit = limit;
            this.limitForeignKey = foreignTableName;
            return this;
        }

        public Client Offset(int offset, string foreignTableName = null)
        {
            this.offset = offset;
            this.offsetForeignKey = foreignTableName;
            return this;
        }

        public Task<ModeledResponse<T>> Insert<T>(T model, InsertOptions options = null) where T : BaseModel, new()
        {
            method = HttpMethod.Post;
            if (options == null)
                options = new InsertOptions();

            var headers = new Dictionary<string, string>
            {
                { "Prefer", options.Upsert ? "return=representation,resolution=merge-duplicates" : "return=representation"}
            };

            var request = Request<T>(method, model, headers);

            Clear();

            return request;
        }

        public Task<ModeledResponse<T>> Update<T>(T model) where T : BaseModel, new()
        {
            method = HttpMethod.Patch;

            var headers = new Dictionary<string, string>
            {
                { "Prefer", "return=representation"}
            };

            var request = Request<T>(method, model, headers);

            Clear();

            return request;
        }

        public Task Delete()
        {
            method = HttpMethod.Delete;

            var request = Request(method, null, null);

            Clear();

            return request;
        }

        public Task Single()
        {
            method = HttpMethod.Get;
            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/vnd.pgrst.object+json" },
                { "Prefer", "return=representation"}
            };

            Clear();
            return null;
        }

        public Task<ModeledResponse<T>> Get<T>()
        {
            var request = Request<T>(method, null, null);
            Clear();
            return request;
        }


        public string GenerateUrl()
        {
            var builder = new UriBuilder($"{BaseUrl}/{tableName}");
            var query = HttpUtility.ParseQueryString(builder.Query);

            foreach (var param in options.QueryParams)
            {
                query[param.Key] = param.Value;
            }

            foreach (var filter in filters)
            {
                var attr = Attribute.GetCustomAttribute(filter.Op.GetType(), typeof(MapToAttribute));
                if (attr is MapToAttribute asAttribute)
                {
                    switch (filter.Op)
                    {
                        case Operator.Like:
                        case Operator.ILike:
                            query[filter.Property] = $"{asAttribute.Mapping}.{filter.Criteria.Replace(" % ", " * ")}";
                            break;
                        default:
                            query[filter.Property] = $"{asAttribute.Mapping}.{filter.Criteria}";
                            break;
                    }
                }
            }

            foreach (var orderer in orderers)
            {
                var attr = Attribute.GetCustomAttribute(orderer.NullPosition.GetType(), typeof(MapToAttribute));
                if (attr is MapToAttribute asAttribute)
                {
                    var key = orderer.ForeignTable != null ? $"{orderer.ForeignTable}.order" : "order";
                    query[key] = $"{orderer.Column}.{orderer.Ordering}.{asAttribute.Mapping}";
                }
            }

            if (limit != int.MinValue)
            {
                var key = limitForeignKey != null ? $"{limitForeignKey}.limit" : "limit";
                query[key] = limit.ToString();
            }

            if (offset != int.MinValue)
            {
                var key = offsetForeignKey != null ? $"{offsetForeignKey}.offset" : "offset";
                query[key] = offset.ToString();
            }

            builder.Query = query.ToString();
            return builder.Uri.ToString();
        }

        public Dictionary<string, string> PrepareRequestData(object data) => JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(data));

        public Dictionary<string, string> PrepareRequestHeaders(Dictionary<string, string> headers = null)
        {
            if (headers == null)
                headers = new Dictionary<string, string>();

            if (options.Schema != null)
            {
                if (method == HttpMethod.Get)
                    headers.Add("Accept-Profile", options.Schema);
                else
                    headers.Add("Content-Profile", options.Schema);
            }

            if (authorization.ApiKey != null)
            {
                headers.Add("apikey", authorization.ApiKey);
                headers.Add("Authorization", $"Bearer {authorization.ApiKey}");
            }

            if (authorization.Username != null && authorization.Password != null)
            {
                var header = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{authorization.Username}:{authorization.Password}"));
                headers.Add("Authorization", $"Basic {header}");
            }

            if (rangeFrom != int.MinValue)
            {
                headers.Add("Range-Unit", "items");
                headers.Add("Range", $"{rangeFrom}-{(rangeTo != int.MinValue ? rangeTo.ToString() : null)}");
            }

            return headers;
        }

        public void Clear()
        {
            tableName = null;
            columnQuery = null;

            filters.Clear();
            orderers.Clear();

            rangeFrom = int.MinValue;
            rangeTo = int.MinValue;

            limit = int.MinValue;
            limitForeignKey = null;

            offset = int.MinValue;
            offsetForeignKey = null;
        }

        private Task<BaseResponse> Request(HttpMethod method, object data, Dictionary<string, string> headers = null)
        {
            return MakeRequest(method, GenerateUrl(), PrepareRequestData(data), PrepareRequestHeaders(headers));
        }

        private Task<ModeledResponse<T>> Request<T>(HttpMethod method, object data, Dictionary<string, string> headers = null)
        {
            return MakeRequest<T>(method, GenerateUrl(), PrepareRequestData(data), PrepareRequestHeaders(headers));
        }
    }
}
