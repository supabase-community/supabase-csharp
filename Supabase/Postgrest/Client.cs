using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Supabase.Models;
using Supabase.Postgrest.Responses;
using static Supabase.Postgrest.Helpers;

namespace Supabase.Postgrest
{
    public class Client
    {
        private string baseUrl;
        private string apiKey;

        private HttpMethod method;
        private ClientOptions options;

        private string tableName;
        private string columnQuery;

        private List<Filter> filters = new List<Filter>();
        private List<Orderer> orderers = new List<Orderer>();

        private int rangeFrom = int.MinValue;
        private int rangeTo = int.MinValue;

        private int limit = int.MinValue;
        private string limitForeignKey;

        private int offset = int.MinValue;
        private string offsetForeignKey;


        public Client(string baseUrl, string apiKey, ClientOptions options)
        {
            this.baseUrl = baseUrl;
            this.apiKey = apiKey;
            this.options = options;
        }

        public Client From(string tableName)
        {
            this.tableName = tableName;
            return this;
        }

        public Client Filter(string columnName, Operator op, string criteria)
        {
            filters.Add(new Filter(columnName, op, criteria));
            return this;
        }

        public Client Match(string query)
        {
            return this;
        }

        public Client Order(string property, Ordering ordering, NullPosition nullPosition = NullPosition.First)
        {
            orderers.Add(new Orderer(property, ordering, nullPosition));
            return this;
        }

        public Client Range(int from)
        {
            this.rangeFrom = from;
            return this;
        }

        public Client Range(int from, int to)
        {
            this.rangeFrom = from;
            this.rangeTo = to;
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


        private Task<ModeledResponse<T>> Request<T>(HttpMethod method, object data, Dictionary<string, string> headers = null)
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

            var toJson = JsonConvert.SerializeObject(data);
            var toDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(toJson);

            return MakeRequest<T>(method, GenerateUrl(), toDictionary, headers);
        }

        private Task<BaseResponse> Request(HttpMethod method, object data, Dictionary<string, string> headers = null)
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

            var json = JsonConvert.SerializeObject(data);
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            return MakeRequest(method, GenerateUrl(), dictionary, headers);
        }

        private string GenerateUrl()
        {
            var url = $"{baseUrl}/{tableName}?apikey={apiKey}";

            foreach (var filter in filters)
            {
                var attr = Attribute.GetCustomAttribute(filter.Op.GetType(), typeof(ProcessAsAttribute));
                if (attr is ProcessAsAttribute asAttribute)
                {
                    switch (filter.Op)
                    {
                        case Operator.Like:
                        case Operator.ILike:
                            url += $"{filter.Property}={asAttribute.Operator}.{filter.Criteria.Replace("%", "*")}";
                            break;
                        case Operator.In:

                            break;
                        default:
                            url += $"{filter.Property}={asAttribute.Operator}.{filter.Criteria}";
                            break;
                    }
                }
            }

            return url;
        }

        private void Clear()
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
    }

    public class ClientOptions
    {
        public string Schema { get; set; }
        public Dictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();
    }

    public class InsertOptions
    {
        public bool Upsert { get; set; } = false;
    }

    public class Filter
    {
        public string Property { get; set; }
        public Operator Op { get; set; }
        public string Criteria { get; set; }

        public Filter(string property, Operator op, string criteria)
        {
            Property = property;
            Op = op;
            Criteria = criteria;
        }
    }

    public class Orderer
    {
        public string Property { get; set; }
        public Ordering Ordering { get; set; }
        public NullPosition NullPosition { get; set; }

        public Orderer(string property, Ordering ordering, NullPosition nullPosition)
        {
            Property = property;
            Ordering = ordering;
            NullPosition = nullPosition;
        }
    }
}
