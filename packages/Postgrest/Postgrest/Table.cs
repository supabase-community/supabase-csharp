using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Supabase.Postgrest.Extensions;
using Supabase.Core.Attributes;
using Supabase.Core.Extensions;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Linq;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Responses;
using static Supabase.Postgrest.Constants;

namespace Supabase.Postgrest
{
    /// <summary>
    /// Class created from a model derived from `BaseModel` that can generate query requests to a Postgrest Endpoint.
    /// 
    /// Representative of a `USE $TABLE` command.
    /// </summary>
    /// <typeparam name="TModel">Model derived from `BaseModel`.</typeparam>
    public class Table<TModel> : IPostgrestTable<TModel> where TModel : BaseModel, new()
    {
        /// <inheritdoc />
        public string BaseUrl { get; }

        /// <inheritdoc />
        public string TableName { get; }

        /// <inheritdoc />
        public Func<Dictionary<string, string>>? GetHeaders { get; set; }

        private readonly ClientOptions _options;
        private readonly JsonSerializerSettings _serializerSettings;

        private HttpMethod _method = HttpMethod.Get;

        #region Pending Query State

        private string? _columnQuery;

        private readonly List<IPostgrestQueryFilter> _filters = new();
        private readonly List<QueryOrderer> _orderers = new();
        private readonly List<string> _columns = new();

        private readonly Dictionary<object, object?> _setData = new();

        private readonly List<ReferenceAttribute> _references = new();

        private int _rangeFrom = int.MinValue;
        private int _rangeTo = int.MinValue;

        private int _limit = int.MinValue;
        private string? _limitForeignKey;

        private int _offset = int.MinValue;
        private string? _offsetForeignKey;

        private string? _onConflict;

        #endregion

        /// <summary>
        /// Typically called from the Client `new Client.Table&lt;ModelType&gt;`
        /// </summary>
        /// <param name="baseUrl">Api Endpoint (ex: "http://localhost:8000"), no trailing slash required.</param>
        /// <param name="serializerSettings"></param>
        /// <param name="options">Optional client configuration.</param>
        public Table(string baseUrl, JsonSerializerSettings serializerSettings, ClientOptions? options = null)
        {
            BaseUrl = baseUrl;

            _options = options ?? new ClientOptions();
            _serializerSettings = serializerSettings;

            foreach (var property in typeof(TModel).GetProperties())
            {
                var attrs = property.GetCustomAttributes(typeof(ReferenceAttribute), true);

                foreach (ReferenceAttribute attr in attrs)
                {
                    attr.ParseProperties(new List<ReferenceAttribute> { attr });
                    _references.Add(attr);
                }
            }

            TableName = FindTableName();
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Filter<TCriterion>(Expression<Func<TModel, object>> predicate, Operator op,
            TCriterion? criterion)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            return Filter(visitor.Columns.First(), op, criterion);
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Filter<TCriterion>(string columnName, Operator op, TCriterion? criterion)
        {
            switch (criterion)
            {
                case null:
                    switch (op)
                    {
                        case Operator.Equals:
                        case Operator.Is:
                            _filters.Add(new QueryFilter(columnName, Operator.Is, QueryFilter.NullVal));
                            break;
                        case Operator.Not:
                        case Operator.NotEqual:
                            _filters.Add(new QueryFilter(columnName, Operator.Not,
                                new QueryFilter(columnName, Operator.Is, QueryFilter.NullVal)));
                            break;
                        default:
                            throw new PostgrestException(
                                    "NOT filters must use the `Equals`, `Is`, `Not` or `NotEqual` operators")
                                { Reason = FailureHint.Reason.InvalidArgument };
                    }

                    return this;
                case string stringCriterion:
                    _filters.Add(new QueryFilter(columnName, op, stringCriterion));
                    return this;
                case int intCriterion:
                    _filters.Add(new QueryFilter(columnName, op, intCriterion));
                    return this;
                case long longCriterion:
                    _filters.Add(new QueryFilter(columnName, op, longCriterion));
                    return this;
                case float floatCriterion:
                    _filters.Add(new QueryFilter(columnName, op, floatCriterion));
                    return this;
                case IDictionary dictCriteria:
                    _filters.Add(new QueryFilter(columnName, op, dictCriteria));
                    return this;
                case IList listCriteria:
                    _filters.Add(new QueryFilter(columnName, op, listCriteria));
                    return this;
                case IntRange rangeCriteria:
                    _filters.Add(new QueryFilter(columnName, op, rangeCriteria));
                    return this;
                case FullTextSearchConfig fullTextSearchCriteria:
                    _filters.Add(new QueryFilter(columnName, op, fullTextSearchCriteria));
                    return this;
                case DateTime dtSearchCriteria:
                    _filters.Add(new QueryFilter(columnName, op, dtSearchCriteria));
                    return this;
                case DateTimeOffset dtoSearchCriteria:
                    _filters.Add(new QueryFilter(columnName, op, dtoSearchCriteria));
                    return this;
                default:
                    throw new PostgrestException(
                        "Unknown criterion type, is it of type `string`, `int`, `long`, `float`, `List`, `DateTime`, `DateTimeOffset`, `Dictionary<string, object>`, `FullTextSearchConfig`, or `Range`?")
                    {
                        Reason = FailureHint.Reason.InvalidArgument
                    };
            }
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Not(IPostgrestQueryFilter filter)
        {
            _filters.Add(new QueryFilter(Operator.Not, filter));
            return this;
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Not<TCriterion>(string columnName, Operator op, TCriterion? criterion) =>
            Not(new QueryFilter(columnName, op, criterion));

        /// <inheritdoc />
        public IPostgrestTable<TModel> Not<TCriterion>(Expression<Func<TModel, object>> predicate, Operator op,
            TCriterion? criterion)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            return Not(new QueryFilter(visitor.Columns.First(), op, criterion));
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Not<TCriterion>(string columnName, Operator op, List<TCriterion> criteria) =>
            Not(new QueryFilter(columnName, op, criteria.Cast<object>().ToList()));

        /// <inheritdoc />
        public IPostgrestTable<TModel> Not<TCriterion>(Expression<Func<TModel, object>> predicate, Operator op,
            List<TCriterion> criteria)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            return Not(new QueryFilter(visitor.Columns.First(), op, criteria.Cast<object>().ToList()));
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Not(string columnName, Operator op, Dictionary<string, object> criteria) =>
            Not(new QueryFilter(columnName, op, criteria));

        /// <inheritdoc />
        public IPostgrestTable<TModel> Not(Expression<Func<TModel, object>> predicate, Operator op,
            Dictionary<string, object> criteria)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            return Not(new QueryFilter(visitor.Columns.First(), op, criteria));
        }


        /// <inheritdoc />
        public IPostgrestTable<TModel> And(List<IPostgrestQueryFilter> filters)
        {
            _filters.Add(new QueryFilter(Operator.And, filters));
            return this;
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Or(List<IPostgrestQueryFilter> filters)
        {
            _filters.Add(new QueryFilter(Operator.Or, filters));
            return this;
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Match(TModel model)
        {
            foreach (var kvp in model.PrimaryKey)
            {
                _filters.Add(new QueryFilter(kvp.Key.ColumnName, Operator.Equals, kvp.Value));
            }

            return this;
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Match(Dictionary<string, string> query)
        {
            foreach (var param in query)
            {
                _filters.Add(new QueryFilter(param.Key, Operator.Equals, param.Value));
            }

            return this;
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Order(Expression<Func<TModel, object>> predicate, Ordering ordering,
            NullPosition nullPosition = NullPosition.First)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            return Order(visitor.Columns.First(), ordering, nullPosition);
        }


        /// <inheritdoc />
        public IPostgrestTable<TModel> Order(string column, Ordering ordering, NullPosition nullPosition = NullPosition.First)
        {
            _orderers.Add(new QueryOrderer(null, column, ordering, nullPosition));
            return this;
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Order(string foreignTable, string column, Ordering ordering,
            NullPosition nullPosition = NullPosition.First)
        {
            _orderers.Add(new QueryOrderer(foreignTable, column, ordering, nullPosition));
            return this;
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Range(int from)
        {
            _rangeFrom = from;
            return this;
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Range(int from, int to)
        {
            _rangeFrom = from;
            _rangeTo = to;
            return this;
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Select(string columnQuery)
        {
            _method = HttpMethod.Get;
            _columnQuery = columnQuery;
            return this;
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Select(Expression<Func<TModel, object[]>> predicate)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException(
                    "Unable to find column(s) to select from the given predicate, did you return an array of Model Properties?");

            return Select(string.Join(",", visitor.Columns));
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> Where(Expression<Func<TModel, bool>> predicate)
        {
            var visitor = new WhereExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.ConstantValue == true)
                return this;

            if (visitor.ConstantValue == false)
                throw new ArgumentException(
                    "The supplied predicate always evaluates to false, so no row would ever match. Evaluate the condition outside of `Where` and build the query conditionally instead.");

            if (visitor.Filter == null)
                throw new ArgumentException(
                    "Unable to parse the supplied predicate, did you return a predicate where each left hand of the condition is a Model property?");

            _filters.Add(visitor.Filter);

            return this;
        }


        /// <inheritdoc />
        public IPostgrestTable<TModel> Limit(int limit, string? foreignTableName = null)
        {
            _limit = limit;
            _limitForeignKey = foreignTableName;
            return this;
        }


        /// <inheritdoc />
        public IPostgrestTable<TModel> OnConflict(string columnName)
        {
            _onConflict = columnName;
            return this;
        }

        /// <inheritdoc />
        public IPostgrestTable<TModel> OnConflict(Expression<Func<TModel, object>> predicate)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            OnConflict(visitor.Columns.First());

            return this;
        }


        /// <inheritdoc />
        public IPostgrestTable<TModel> Columns(string[] columns)
        {
            foreach (var column in columns)
                _columns.Add(column);

            return this;
        }


        /// <inheritdoc />
        public IPostgrestTable<TModel> Columns(Expression<Func<TModel, object[]>> predicate)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return an array of references to a Model column.");

            return Columns(visitor.Columns.ToArray());
        }


        /// <inheritdoc />
        public IPostgrestTable<TModel> Offset(int offset, string? foreignTableName = null)
        {
            _offset = offset;
            _offsetForeignKey = foreignTableName;
            return this;
        }


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Insert(TModel model, QueryOptions? options = null,
            CancellationToken cancellationToken = default) => PerformInsert(model, options, cancellationToken);


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Insert(ICollection<TModel> models, QueryOptions? options = null,
            CancellationToken cancellationToken = default) => PerformInsert(models, options, cancellationToken);


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Upsert(TModel model, QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            // Enforce Upsert
            options.Upsert = true;

            return PerformInsert(model, options, cancellationToken);
        }


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Upsert(ICollection<TModel> model, QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            // Enforce Upsert
            options.Upsert = true;

            return PerformInsert(model, options, cancellationToken);
        }


        /// <inheritdoc />
        public IPostgrestTable<TModel> Set(Expression<Func<TModel, object>> keySelector, object? value)
        {
            var visitor = new SetExpressionVisitor();
            visitor.Visit(keySelector);

            if (visitor.Column == null || visitor.ExpectedType == null)
                throw new ArgumentException(
                    "Expression should return a KeyValuePair with a key of a Model Property and a value.");

            if (value == null && visitor.ExpectedType != typeof(string))
            {
                if (Nullable.GetUnderlyingType(visitor.ExpectedType) == null)
                    throw new ArgumentException(
                        $"Expected Value to be of Type: {visitor.ExpectedType.Name}, instead received: {null}.");
            }
            else if (value != null && !visitor.ExpectedType.IsInstanceOfType(value))
            {
                throw new ArgumentException(string.Format("Expected Value to be of Type: {0}, instead received: {1}.",
                    visitor.ExpectedType.Name, value.GetType().Name));
            }

            _setData.Add(visitor.Column, value);

            return this;
        }


        /// <inheritdoc />
        public IPostgrestTable<TModel> Set(Expression<Func<TModel, KeyValuePair<object, object?>>> keyValuePairExpression)
        {
            var visitor = new SetExpressionVisitor();
            visitor.Visit(keyValuePairExpression);

            if (visitor.Column == null || visitor.Value == default)
                throw new ArgumentException(
                    "Expression should return a KeyValuePair with a key of a Model Property and a value.");

            _setData.Add(visitor.Column, visitor.Value);

            return this;
        }


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Update(QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            if (_setData.Keys.Count == 0)
                throw new ArgumentException("No data has been set to update, was `Set` called?");

            _method = new HttpMethod("PATCH");

            var request = Send<TModel>(_method, _setData, options.ToHeaders(), cancellationToken, isUpdate: true);

            Clear();

            return request;
        }


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Update(TModel model, QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            _method = new HttpMethod("PATCH");

            Match(model);

            var request = Send<TModel>(_method, model, options.ToHeaders(), cancellationToken, isUpdate: true);

            Clear();

            return request;
        }


        /// <inheritdoc />
        public Task Delete(QueryOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            _method = HttpMethod.Delete;

            var request = Send(_method, null, options.ToHeaders(), cancellationToken);

            Clear();

            return request;
        }


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Delete(TModel model, QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            _method = HttpMethod.Delete;

            Match(model);

            var request = Send<TModel>(_method, null, options.ToHeaders(), cancellationToken);
            Clear();
            return request;
        }


        /// <inheritdoc />
        public async Task<int> Count(CountType type, CancellationToken cancellationToken = default)
        {
            _method = HttpMethod.Head;

            var attr = type.GetAttribute<MapToAttribute>();

            var headers = new Dictionary<string, string>
            {
                { "Prefer", $"count={attr?.Mapping}" }
            };

            var request = Send(_method, null, headers, cancellationToken);
            Clear();

            var response = await request;
            var countStr = response.ResponseMessage?.Content.Headers.GetValues("Content-Range").FirstOrDefault();

            // Returns X-Y/COUNT [0-3/4]
            return int.Parse(countStr?.Split('/')[1] ?? throw new InvalidOperationException());
        }


        /// <inheritdoc />
        public async Task<TModel?> Single(CancellationToken cancellationToken = default)
        {
            _method = HttpMethod.Get;
            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/vnd.pgrst.object+json" },
                { "Prefer", "return=representation" }
            };

            var request = Send<TModel>(_method, null, headers, cancellationToken);

            Clear();

            try
            {
                var result = await request;
                return result.Models.FirstOrDefault();
            }
            catch (PostgrestException e)
            {
                if (e.Response!.StatusCode == HttpStatusCode.NotAcceptable)
                    return null;

                throw;
            }
        }

        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Get(CancellationToken cancellationToken = default, CountType type = CountType.Estimated)
        {
            var attr = type.GetAttribute<MapToAttribute>();
            
            var headers = new Dictionary<string, string>
            {
                { "Prefer", $"count={attr?.Mapping}" }
            };
            
            var request = Send<TModel>(_method, null, headers, cancellationToken);
            Clear();
            
            return request;
        }

        /// <summary>
        /// Generates the encoded URL with defined query parameters that will be sent to the Postgrest API.
        /// </summary>
        /// <returns></returns>
        public string GenerateUrl()
        {
            var builder = new UriBuilder($"{BaseUrl}/{TableName}");
            var query = HttpUtility.ParseQueryString(builder.Query);

            foreach (var param in _options.QueryParams)
                query.Add(param.Key, param.Value);

            if (_options.Headers.TryGetValue("apikey", out var header))
                query.Add("apikey", header);

            if (_columns.Count > 0)
                query["columns"] = string.Join(",", _columns);

            foreach (var parsedFilter in _filters.Select(PrepareFilter))
                query.Add(parsedFilter.Key, parsedFilter.Value);

            if (_orderers.Count > 0)
            {
                var order = new StringBuilder();

                foreach (var orderer in _orderers)
                {
                    var nullPosAttr = orderer.NullPosition.GetAttribute<MapToAttribute>();
                    var orderingAttr = orderer.Ordering.GetAttribute<MapToAttribute>();

                    if (nullPosAttr == null || orderingAttr == null) continue;

                    if (order.Length > 0)
                        order.Append(",");

                    var selector = !string.IsNullOrEmpty(orderer.ForeignTable)
                        ? orderer.ForeignTable + "(" + orderer.Column + ")"
                        : orderer.Column;
                    
                    order.Append($"{selector}.{orderingAttr.Mapping}.{nullPosAttr.Mapping}");
                }

                query.Add("order", order.ToString());
            }

            if (!string.IsNullOrEmpty(_columnQuery))
                query["select"] = Regex.Replace(_columnQuery!, @"\s", "");

            if (_references.Count > 0)
            {
                query["select"] ??= "*";

                foreach (var reference in _references)
                {
                    if ((_method == HttpMethod.Get && !reference.IncludeInQuery) ||
                        (_method == HttpMethod.Post && reference.IgnoreOnInsert) ||
                        (_method == new HttpMethod("PATCH") && reference.IgnoreOnUpdate) ||
                        _method == HttpMethod.Delete) continue;

                    var columns = string.Join(",", reference.Columns.ToArray());

                    if (!string.IsNullOrEmpty(reference.ForeignKey))
                    {
                        if (reference.UseInnerJoin)
                            query["select"] += $",{reference.ColumnName}:{reference.ForeignKey}!inner({columns})";
                        else
                            query["select"] += $",{reference.ColumnName}:{reference.ForeignKey}({columns})";
                    }
                    else
                    {
                        if (reference.UseInnerJoin)
                            query["select"] += $",{reference.TableName}!inner({columns})";
                        else
                            query["select"] += $",{reference.TableName}({columns})";
                    }
                }
            }

            if (!string.IsNullOrEmpty(_onConflict))
                query["on_conflict"] = _onConflict;

            if (_limit != int.MinValue)
            {
                var key = _limitForeignKey != null ? $"{_limitForeignKey}.limit" : "limit";
                query[key] = _limit.ToString();
            }

            if (_offset != int.MinValue)
            {
                var key = _offsetForeignKey != null ? $"{_offsetForeignKey}.offset" : "offset";
                query[key] = _offset.ToString();
            }

            builder.Query = query.ToString();
            return builder.Uri.ToString();
        }

        /// <summary>
        /// Settings for re-parsing the already-serialized payload into the loose object graph that is sent
        /// on the wire. <see cref="DateParseHandling.None"/> keeps date/time values as their serialized
        /// strings so they pass through verbatim; parsing them back into <see cref="DateTime"/> would strip
        /// the column-level converters and let the default handling shift them (i.e. an unspecified-kind
        /// `date` to the previous day in timezones ahead of UTC).
        /// </summary>
        private readonly JsonSerializerSettings _passthroughSettings =
            new() { DateParseHandling = DateParseHandling.None };

        /// <summary>
        /// Transforms an object into a string mapped list/dictionary using `JsonSerializerSettings`.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="isInsert"></param>
        /// <param name="isUpdate"></param>
        /// <param name="isUpsert"></param>
        /// <returns></returns>
        private object? PrepareRequestData(object? data, bool isInsert = false, bool isUpdate = false,
            bool isUpsert = false)
        {
            if (data == null) return new Dictionary<string, string>();

            // Specified in constructor;
            var resolver = (PostgrestContractResolver)_serializerSettings.ContractResolver!;

            resolver.SetState(isInsert, isUpdate, isUpsert);

            var serialized = JsonConvert.SerializeObject(data, _serializerSettings);

            resolver.SetState();

            // Check if data is a Collection for the Insert Bulk case
            if (data is ICollection<TModel>)
                return JsonConvert.DeserializeObject<List<object>>(serialized, _passthroughSettings);

            return JsonConvert.DeserializeObject<Dictionary<string, object>>(serialized, _passthroughSettings);
        }

        /// <summary>
        /// Transforms the defined filters into the expected Postgrest format.
        ///
        /// See: http://postgrest.org/en/v7.0.0/api.html#operators
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        internal KeyValuePair<string, string> PrepareFilter(IPostgrestQueryFilter filter)
        {
            var asAttribute = filter.Op.GetAttribute<MapToAttribute>();
            var strBuilder = new StringBuilder();

            if (asAttribute == null)
                return new KeyValuePair<string, string>();

            switch (filter.Op)
            {
                case Operator.Or:
                case Operator.And:
                    if (filter.Criteria is List<IPostgrestQueryFilter> subFilters)
                    {
                        var list = new List<KeyValuePair<string, string>>();
                        foreach (var subFilter in subFilters)
                        {
                            if (subFilter == null)
                                throw new ArgumentException(
                                    $"Expected all filters supplied to a `{filter.Op}` filter to be non-null.");

                            list.Add(PrepareFilter(subFilter));
                        }

                        foreach (var preppedFilter in list)
                            strBuilder.Append($"{preppedFilter.Key}.{preppedFilter.Value},");

                        return new KeyValuePair<string, string>(asAttribute.Mapping,
                            $"({strBuilder.ToString().Trim(',')})");
                    }

                    break;
                case Operator.Not:
                    if (filter.Criteria is QueryFilter notFilter)
                        return NegatePreparedFilter(notFilter, PrepareFilter(notFilter));

                    break;
                case Operator.Like:
                case Operator.ILike:
                    if (filter is { Criteria: string likeCriteria, Property: not null })
                    {
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{likeCriteria.Replace("%", "*")}");
                    }

                    break;
                case Operator.In:
                    if (filter is { Criteria: IList inCriteria, Property: not null })
                    {
                        foreach (var item in inCriteria)
                            strBuilder.Append($"\"{item}\",");

                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.({strBuilder.ToString().Trim(',')})");
                    }

                    if (filter is { Criteria: IDictionary inDictCriteria, Property: not null })
                    {
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{JsonConvert.SerializeObject(inDictCriteria)}");
                    }

                    break;
                case Operator.Contains:
                case Operator.ContainedIn:
                case Operator.Overlap:
                    switch (filter.Criteria)
                    {
                        case IList listCriteria when filter.Property != null:
                        {
                            foreach (var item in listCriteria)
                                strBuilder.Append($"{item},");

                            return new KeyValuePair<string, string>(filter.Property,
                                $"{asAttribute.Mapping}.{{{strBuilder.ToString().Trim(',')}}}");
                        }
                        case IDictionary dictCriteria when filter.Property != null:
                            return new KeyValuePair<string, string>(filter.Property,
                                $"{asAttribute.Mapping}.{JsonConvert.SerializeObject(dictCriteria)}");
                        case IntRange rangeCriteria when filter.Property != null:
                            return new KeyValuePair<string, string>(filter.Property,
                                $"{asAttribute.Mapping}.{rangeCriteria.ToPostgresString()}");
                    }

                    break;
                case Operator.StrictlyLeft:
                case Operator.StrictlyRight:
                case Operator.NotRightOf:
                case Operator.NotLeftOf:
                case Operator.Adjacent:
                    if (filter is { Criteria: IntRange rangeCriterion, Property: not null })
                    {
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{rangeCriterion.ToPostgresString()}");
                    }

                    break;
                case Operator.FTS:
                case Operator.PHFTS:
                case Operator.PLFTS:
                case Operator.WFTS:
                    if (filter is { Criteria: FullTextSearchConfig searchConfig, Property: not null })
                    {
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}({searchConfig.Config}).{searchConfig.QueryText}");
                    }

                    break;
                default:
                    return new KeyValuePair<string, string>(filter.Property ?? "",
                        $"{asAttribute.Mapping}.{filter.Criteria}");
            }

            return new KeyValuePair<string, string>();
        }

        /// <summary>
        /// Applies a `not.` negation to an already-prepared filter. A negated logical group is expressed as
        /// `not.and=(...)` / `not.or=(...)`, with the `not.` prefixing the key; a negated column filter keeps
        /// the `not.` on the value (`not.eq.foo`).
        /// </summary>
        private static KeyValuePair<string, string> NegatePreparedFilter(QueryFilter inner, KeyValuePair<string, string> prepared) =>
            inner.Op is Operator.And or Operator.Or
                ? new KeyValuePair<string, string>($"not.{prepared.Key}", prepared.Value)
                : new KeyValuePair<string, string>(prepared.Key, $"not.{prepared.Value}");

        /// <inheritdoc />
        public void Clear()
        {
            _columnQuery = null;

            _filters.Clear();
            _orderers.Clear();
            _columns.Clear();
            _setData.Clear();

            _rangeFrom = int.MinValue;
            _rangeTo = int.MinValue;

            _limit = int.MinValue;
            _limitForeignKey = null;

            _offset = int.MinValue;
            _offsetForeignKey = null;

            _onConflict = null;
        }


        /// <summary>
        /// Performs an INSERT Request.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Task<ModeledResponse<TModel>> PerformInsert(object data, QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            _method = HttpMethod.Post;
            options ??= new QueryOptions();

            if (!string.IsNullOrEmpty(options.OnConflict))
                OnConflict(options.OnConflict!);

            var request = Send<TModel>(_method, data, options.ToHeaders(), cancellationToken, isInsert: true,
                isUpsert: options.Upsert);

            Clear();

            return request;
        }

        private Task<BaseResponse> Send(HttpMethod method, object? data, Dictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default, bool isInsert = false,
            bool isUpdate = false, bool isUpsert = false)
        {
            var requestHeaders = Helpers.PrepareRequestHeaders(method, headers, _options, _rangeFrom, _rangeTo);

            if (GetHeaders != null)
            {
                requestHeaders = GetHeaders().MergeLeft(requestHeaders);
            }

            var url = GenerateUrl();
            var preparedData = PrepareRequestData(data, isInsert, isUpdate, isUpsert);

            Hooks.Instance.NotifyOnRequestPreparedHandlers(this, _options, method, url, _serializerSettings,
                preparedData, requestHeaders);

            Debugger.Instance.Log(this,
                $"Request [{method}] at {DateTime.Now.ToLocalTime()}\n" +
                $"Headers:\n\t{JsonConvert.SerializeObject(requestHeaders)}\n" +
                $"Data:\n\t{JsonConvert.SerializeObject(preparedData)}");

            var operation = PostgrestInstrumentation.ResolveOperation(method, isInsert, isUpdate, isUpsert);
            return Helpers.MakeRequest(_options, method, url, _serializerSettings, preparedData, requestHeaders,
                cancellationToken, operation);
        }

        private Task<ModeledResponse<TU>> Send<TU>(HttpMethod method, object? data,
            Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default,
            bool isInsert = false,
            bool isUpdate = false, bool isUpsert = false) where TU : BaseModel, new()
        {
            var requestHeaders = Helpers.PrepareRequestHeaders(method, headers, _options, _rangeFrom, _rangeTo);

            if (GetHeaders != null)
                requestHeaders = GetHeaders().MergeLeft(requestHeaders);

            var url = GenerateUrl();
            var preparedData = PrepareRequestData(data, isInsert, isUpdate, isUpsert);

            Hooks.Instance.NotifyOnRequestPreparedHandlers(this, _options, method, url, _serializerSettings,
                preparedData, requestHeaders);

            Debugger.Instance.Log(this,
                $"Request [{method}] at {DateTime.Now.ToLocalTime()}\n" +
                $"Headers:\n\t{JsonConvert.SerializeObject(requestHeaders)}\n" +
                $"Data:\n\t{JsonConvert.SerializeObject(preparedData)}");

            var operation = PostgrestInstrumentation.ResolveOperation(method, isInsert, isUpdate, isUpsert);
            return Helpers.MakeRequest<TU>(_options, method, url, _serializerSettings, preparedData, requestHeaders,
                GetHeaders, cancellationToken, operation);
        }

        private static string FindTableName(object? obj = null)
        {
            var type = obj == null ? typeof(TModel) : obj is Type t ? t : obj.GetType();
            var attr = Attribute.GetCustomAttribute(type, typeof(TableAttribute));

            if (attr is TableAttribute tableAttr)
            {
                return tableAttr.Name;
            }

            return type.Name;
        }
    }
}