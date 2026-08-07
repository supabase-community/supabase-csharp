using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Linq;
using static Supabase.Postgrest.Constants;

namespace Supabase.Postgrest
{
    /// <summary>
    /// Allow for the expression of a query filter with linq expressions.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TCriterion"></typeparam>
    public class QueryFilter<TModel, TCriterion> : IPostgrestQueryFilter
    {
        /// <inheritdoc />
        public object? Criteria { get; }

        /// <inheritdoc />
        public Operator Op { get; }

        /// <inheritdoc />
        public string? Property { get; }

        /// <summary>
        /// Allows the creation of a Query Filter using a LINQ expression.
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="op"></param>
        /// <param name="criterion"></param>
        /// <exception cref="ArgumentException"></exception>
        public QueryFilter(Expression<Func<TModel, object>> predicate, Operator op, TCriterion? criterion)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            var filter = new QueryFilter(visitor.Columns.First(), op, criterion);

            Criteria = filter.Criteria;
            Op = filter.Op;
            Property = filter.Property;
        }
    }

    /// <inheritdoc />
    public class QueryFilter : IPostgrestQueryFilter
    {
        /// <summary>
        /// String value to be substituted for a null criterion
        /// </summary>
        public const string NullVal = "null";

        /// <inheritdoc />
        public string? Property { get; private set; }

        /// <inheritdoc />
        public Operator Op { get; private set; }

        /// <inheritdoc />
        public object? Criteria { get; private set; }

        /// <summary>
        /// Contractor to use single value filtering.
        /// </summary>
        /// <param name="property">Column name</param>
        /// <param name="op">Operation: And, Equals, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, NotEqual, Is, Adjacent, Not, Like, ILike</param>
        /// <param name="criteria"></param>
        public QueryFilter(string property, Operator op, object? criteria)
        {
            if (criteria is DateTime dateTime)
                criteria = dateTime.ToString("o", CultureInfo.InvariantCulture);
            if (criteria is DateTimeOffset dateTimeOffset)
                criteria = dateTimeOffset.ToString("o", CultureInfo.InvariantCulture);
            
            switch (op)
            {
                case Operator.And:
                case Operator.Equals:
                case Operator.GreaterThan:
                case Operator.GreaterThanOrEqual:
                case Operator.LessThan:
                case Operator.LessThanOrEqual:
                case Operator.NotEqual:
                case Operator.Is:
                case Operator.Adjacent:
                case Operator.Not:
                case Operator.Like:
                case Operator.ILike:
                    Property = property;
                    Op = op;
                    Criteria = criteria;
                    break;
                case Operator.In:
                case Operator.Contains:
                case Operator.ContainedIn:
                case Operator.Overlap:
                    if (criteria is IList or IDictionary)
                    {
                        Property = property;
                        Op = op;
                        Criteria = criteria;
                    }
                    else
                    {
                        throw new PostgrestException(
                                "List or Dictionary must be used supplied as criteria with filters that accept an array of arguments.")
                            { Reason = FailureHint.Reason.InvalidArgument };
                    }
                    break;
                default:
                    throw new PostgrestException("Advanced filters require a constructor with more specific arguments")
                        { Reason = FailureHint.Reason.InvalidArgument };
            }
        }

        /// <summary>
        /// Constructor for Full Text Search.
        /// </summary>
        /// <param name="property">Column Name</param>
        /// <param name="op">Operation: FTS, PHFTS, PLFTS, WFTS</param>
        /// <param name="fullTextSearchConfig"></param>
        public QueryFilter(string property, Operator op, FullTextSearchConfig fullTextSearchConfig)
        {
            switch (op)
            {
                case Operator.FTS:
                case Operator.PHFTS:
                case Operator.PLFTS:
                case Operator.WFTS:
                    Property = property;
                    Op = op;
                    Criteria = fullTextSearchConfig;
                    break;
                default:
                    throw new PostgrestException("Constructor must be called with a full text search operator")
                        { Reason = FailureHint.Reason.InvalidArgument };
            }
        }

        /// <summary>
        /// Constructor for Range Queries.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="op">Operator: Overlap, StrictlyLeft, StrictlyRight, NotRightOf, NotLeftOf, Adjacent</param>
        /// <param name="range"></param>
        public QueryFilter(string property, Operator op, IntRange range)
        {
            switch (op)
            {
                case Operator.Overlap:
                case Operator.Contains:
                case Operator.ContainedIn:
                case Operator.StrictlyLeft:
                case Operator.StrictlyRight:
                case Operator.NotRightOf:
                case Operator.NotLeftOf:
                case Operator.Adjacent:
                    Property = property;
                    Op = op;
                    Criteria = range;
                    break;
                default:
                    throw new PostgrestException("Constructor must be called with a filter that accepts a range")
                    {
                        Reason = FailureHint.Reason.InvalidArgument
                    };
            }
        }

        /// <summary>
        /// Constructor to enable `AND` and `OR` Queries by allowing nested QueryFilters.
        /// </summary>
        /// <param name="op">Operation: And, Or</param>
        /// <param name="filters"></param>
        public QueryFilter(Operator op, List<IPostgrestQueryFilter> filters)
        {
            switch (op)
            {
                case Operator.Or:
                case Operator.And:
                    Op = op;
                    Criteria = filters;
                    break;
                default:
                    throw new PostgrestException("Constructor can only be used with `or` or `and` filters")
                        { Reason = FailureHint.Reason.InvalidArgument };
            }
        }

        /// <summary>
        /// Constructor to enable `NOT` functionality
        /// </summary>
        /// <param name="op">Operation: Not.</param>
        /// <param name="filter"></param>
        public QueryFilter(Operator op, IPostgrestQueryFilter filter)
        {
            switch (op)
            {
                case Operator.Not:
                    Op = op;
                    Criteria = filter;
                    break;
                default:
                    throw new PostgrestException("Constructor can only be used with `not` filter")
                        { Reason = FailureHint.Reason.InvalidArgument };
            }
        }
    }

    /// <summary>
    /// Configuration Object for Full Text Search.
    /// API Reference: http://postgrest.org/en/v7.0.0/api.html?highlight=full%20text%20search#full-text-search
    /// </summary>
    public class FullTextSearchConfig
    {
        /// <summary>
        /// Query Text
        /// </summary>
        [JsonProperty("queryText")]
        public string QueryText { get; private set; }

        /// <summary>
        /// Defaults to english
        /// </summary>
        [JsonProperty("config")]
        public string Config { get; private set; } = "english";

        /// <summary>
        /// Constructor for Full Text Search.
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="config"></param>
        public FullTextSearchConfig(string queryText, string? config)
        {
            QueryText = queryText;

            if (!string.IsNullOrEmpty(config))
                Config = config!;
        }
    }
}