using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Supabase.Extensions;
using Supabase.Models;
using Supabase.Postgrest.Responses;
using static Supabase.Postgrest.Helpers;

namespace Supabase.Postgrest
{
    public class Client<T> where T : BaseModel, new()
    {
        public string BaseUrl { get; private set; }

        private ClientAuthorization authorization;
        private ClientOptions options;

        private HttpMethod method = HttpMethod.Get;

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


        public Client(string baseUrl, ClientAuthorization authorization, ClientOptions options = null)
        {
            BaseUrl = baseUrl;

            if (options == null)
                options = new ClientOptions();

            this.options = options;
            this.authorization = authorization;

            var attr = Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            if (attr is TableAttribute tableAttr)
            {
                tableName = tableAttr.Name;
            }
            else
            {
                tableName = typeof(T).Name;
            }
        }

        public Client<T> Filter(string columnName, Operator op, string criteria)
        {
            filters.Add(new QueryFilter(columnName, op, criteria));
            return this;
        }

        public Client<T> Match(Dictionary<string, string> query)
        {
            return this;
        }

        public Client<T> Order(string column, Ordering ordering, NullPosition nullPosition = NullPosition.First)
        {
            orderers.Add(new QueryOrderer(null, column, ordering, nullPosition));
            return this;
        }

        public Client<T> Order(string foreignTable, string column, Ordering ordering, NullPosition nullPosition = NullPosition.First)
        {
            orderers.Add(new QueryOrderer(foreignTable, column, ordering, nullPosition));
            return this;
        }

        public Client<T> Range(int from)
        {
            rangeFrom = from;
            return this;
        }

        public Client<T> Range(int from, int to)
        {
            rangeFrom = from;
            rangeTo = to;
            return this;
        }

        public Client<T> Select(string columnQuery)
        {
            method = HttpMethod.Get;
            this.columnQuery = columnQuery;
            return this;
        }

        public Client<T> Limit(int limit, string foreignTableName = null)
        {
            this.limit = limit;
            this.limitForeignKey = foreignTableName;
            return this;
        }

        public Client<T> Offset(int offset, string foreignTableName = null)
        {
            this.offset = offset;
            this.offsetForeignKey = foreignTableName;
            return this;
        }

        public Task<ModeledResponse<T>> Insert(T model, InsertOptions options = null)
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

        public Task<ModeledResponse<T>> Update(T model)
        {
            method = HttpMethod.Patch;
            filters.Add(new QueryFilter("id", Operator.Equals, model.Id.ToString()));

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

        public Task Delete(T model)
        {
            method = HttpMethod.Delete;
            Filter("id", Operator.Equals, model.Id.ToString());
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

        public Task<ModeledResponse<T>> Get()
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
                var attr = filter.Op.GetAttribute<MapToAttribute>();
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
                var attr = orderer.NullPosition.GetAttribute<MapToAttribute>();
                if (attr is MapToAttribute asAttribute)
                {
                    var key = !string.IsNullOrEmpty(orderer.ForeignTable) ? $"{orderer.ForeignTable}.order" : "order";
                    query[key] = $"{orderer.Column}.{orderer.Ordering}.{asAttribute.Mapping}";
                }
            }

            if (authorization.Type == ClientAuthorization.AuthorizationType.Token)
            {
                query["apikey"] = authorization.ApiKey;
            }

            if (!string.IsNullOrEmpty(columnQuery))
            {
                query["select"] = Regex.Replace(columnQuery, @"\s", "");
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

            if (!string.IsNullOrEmpty(options.Schema))
            {
                if (method == HttpMethod.Get)
                    headers.Add("Accept-Profile", options.Schema);
                else
                    headers.Add("Content-Profile", options.Schema);
            }

            switch (authorization.Type)
            {
                case ClientAuthorization.AuthorizationType.ApiKey:
                    headers.Add("apikey", authorization.ApiKey);
                    break;
                case ClientAuthorization.AuthorizationType.Token:
                    headers.Add("Authorization", $"Bearer {authorization.Token}");
                    break;
                case ClientAuthorization.AuthorizationType.Basic:
                    var header = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{authorization.Username}:{authorization.Password}"));
                    headers.Add("Authorization", $"Basic {header}");
                    break;
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
