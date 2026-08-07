using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Interfaces;
using static Supabase.Postgrest.Constants;

// ReSharper disable InvalidXmlDocComment

namespace Supabase.Postgrest.Linq

{
    /// <summary>
    /// Helper class for parsing Where linq queries. Supports comparisons, `&amp;&amp;`/`||` groups,
    /// `String`/collection `Contains` (including a captured collection containing a column, which becomes
    /// an `in` filter), bare boolean columns (`x =&gt; x.IsActive`), null checks (`is null`), and negation
    /// (`!`), translating each into the equivalent Postgrest filter; negation wraps its operand in a `not.`
    /// filter.
    /// </summary>
    internal class WhereExpressionVisitor : ExpressionVisitor
    {
        private ParameterExpression? parameter;

        public WhereExpressionVisitor() { }

        private WhereExpressionVisitor(ParameterExpression? parameter) =>
            this.parameter = parameter;

        /// <summary>
        /// The filter resulting from this Visitor, capable of producing nested filters.
        /// </summary>
        public QueryFilter? Filter { get; private set; }

        /// <summary>
        /// Set instead of <see cref="Filter"/> when the predicate (or the visited branch of it) never
        /// references the model and was instead evaluated locally to a boolean
        /// (i.e. `x => filterPredicate == null || filterPredicate(x)` where `filterPredicate` is null).
        /// </summary>
        public bool? ConstantValue { get; private set; }

        /// <inheritdoc />
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            parameter ??= node.Parameters.FirstOrDefault();

            // A predicate that never references the model (i.e. `x => localVariable == null`) can't be
            // translated into a filter - it is evaluated locally instead.
            if (node.Body.Type == typeof(bool) && !ContainsParameter(node.Body))
            {
                ConstantValue = (bool)EvaluateExpression(node.Body)!;
                return node;
            }

            return base.VisitLambda(node);
        }

        /// <summary>
        /// An entry point that will be used to populate <see cref="Filter"/>.
        ///
        /// Invoked like:
        ///		`Table&lt;Movies&gt;().Where(x => x.Name == "Top Gun").Get();`
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var op = GetMappedOperator(node);

            // In the event this is a nested expression (n.Name == "Example" || n.Id = 3)
            switch (node.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    var shortCircuitValue = node.NodeType is ExpressionType.Or or ExpressionType.OrElse;

                    var (leftConstant, leftFilter) = VisitBranch(node.Left);

                    // Follow C#'s short-circuit semantics: `true || anything` and `false && anything`
                    // never evaluate their right side (i.e. `filterPredicate == null || filterPredicate(x)`).
                    if (leftConstant == shortCircuitValue)
                    {
                        ConstantValue = leftConstant;
                        return node;
                    }

                    var (rightConstant, rightFilter) = VisitBranch(node.Right);

                    // A non-short-circuiting constant (`false ||` / `true &&`) reduces to the other side.
                    if (leftConstant != null)
                    {
                        ConstantValue = rightConstant;
                        Filter = rightFilter;
                        return node;
                    }

                    if (rightConstant != null)
                    {
                        if (rightConstant == shortCircuitValue)
                            ConstantValue = rightConstant;
                        else
                            Filter = leftFilter;

                        return node;
                    }

                    Filter = new QueryFilter(op,
                        new List<IPostgrestQueryFilter> { leftFilter!, rightFilter! });

                    return node;
            }

            // Otherwise, the base case.

            string? column = null;
            if (node.Left is MemberExpression leftMember)
            {
                column = GetColumnFromMemberExpression(leftMember);
            } //To handle properly if it's a Convert ExpressionType generally with nullable properties
            else if (node.Left is UnaryExpression leftUnary && leftUnary.NodeType == ExpressionType.Convert &&
                     leftUnary.Operand is MemberExpression leftOperandMember)
            {
                column = GetColumnFromMemberExpression(leftOperandMember);
            }

            if (column == null)
                throw new ArgumentException(
                    $"Left side of expression: '{node}' is expected to be property with a ColumnAttribute or PrimaryKeyAttribute");

            if (ContainsParameter(node.Right))
                throw new ArgumentException($"Unable to translate '{node}': Postgrest cannot compare two model columns to each other. Use a database computed/generated column or an RPC for column-to-column comparisons.");

            if (node.Right is ConstantExpression rightConstantExpression)
            {
                HandleConstantExpression(column, op, rightConstantExpression);
            }
            else if (node.Right is MemberExpression memberExpression)
            {
                HandleMemberExpression(column, op, memberExpression);
            }
            else if (node.Right is NewExpression newExpression)
            {
                HandleNewExpression(column, op, newExpression);
            }
            else if (node.Right is UnaryExpression unaryExpression)
            {
                HandleUnaryExpression(column, op, unaryExpression);
            }

            return node;
        }

        /// <summary>
        /// Handles a logical negation (i.e. `x => !(x.Name == "foo")`). The operand is visited as its own
        /// branch and the resulting filter is wrapped in a `not.` filter; a locally evaluated operand
        /// (i.e. `x => !someLocalBool`) simply flips its boolean value.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType != ExpressionType.Not)
                return base.VisitUnary(node);

            var (constant, filter) = VisitBranch(node.Operand);

            if (constant != null)
                ConstantValue = !constant;
            else
                Filter = new QueryFilter(Operator.Not, filter!);

            return node;
        }

        /// <summary>
        /// Handles a boolean column used directly as a predicate (i.e. `x => x.IsActive`, or negated via
        /// <see cref="VisitUnary"/> `x => !x.IsActive`), translating it into a `column.eq.true` filter.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Type != typeof(bool) || !ContainsParameter(node))
                return base.VisitMember(node);

            Filter = new QueryFilter(GetColumnFromMemberExpression(node), Operator.Equals, true);
            return node;
        }

        /// <summary>
        /// Visits one side of an `AND`/`OR` expression, producing either a <see cref="QueryFilter"/> or,
        /// when the branch never references the model, its locally evaluated boolean value.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private (bool? Constant, QueryFilter? Filter) VisitBranch(Expression expression)
        {
            if (expression.Type == typeof(bool) && !ContainsParameter(expression))
                return ((bool)EvaluateExpression(expression)!, null);

            var visitor = new WhereExpressionVisitor(parameter);
            visitor.Visit(expression);

            if (visitor.ConstantValue != null)
                return (visitor.ConstantValue, null);

            if (visitor.Filter == null)
                throw new ArgumentException(
                    $"Unable to translate expression '{expression}' into a Postgrest filter. If the condition depends on values that are not model columns (i.e. invoking a delegate), evaluate it outside of `Where` and build the query conditionally instead.");

            return (null, visitor.Filter);
        }

        /// <summary>
        /// Translates a `Contains` call. Two shapes are supported, distinguished by which side references
        /// the model: a model column containing a constant (`x =&gt; x.Tags.Contains("a")` → `cs`/`like`) and
        /// a captured collection containing a model column (`x =&gt; ids.Contains(x.Id)` → `in`). The latter
        /// works for both instance (`List.Contains`) and static (`Enumerable.Contains`) calls.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name != nameof(Enumerable.Contains))
                throw new NotImplementedException("Unsupported method");

            var (collection, item) = DeconstructContains(node);
            var collectionIsColumn = ContainsParameter(collection);
            var itemIsColumn = ContainsParameter(item);
            if (collectionIsColumn && !itemIsColumn)
                VisitColumnContains(collection, item);
            else if (itemIsColumn && !collectionIsColumn)
                VisitCapturedContains(collection, item);
            else
                throw new ArgumentException($"Unable to translate '{node}' into a Postgrest filter. `Contains` is supported as a model column containing a constant (i.e. `x => x.Tags.Contains(\"a\")`) or a captured collection containing a model column (i.e. `x => ids.Contains(x.Id)`).");

            return node;
        }

        /// <summary>
        /// Splits a `Contains` call into its collection and item, whether it is an instance call
        /// (`collection.Contains(item)`) or a static `Enumerable.Contains(collection, item)`.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static (Expression Collection, Expression Item) DeconstructContains(MethodCallExpression node) =>
            node.Object == null ? (node.Arguments[0], node.Arguments[1]) : (node.Object, node.Arguments[0]);

        /// <summary>
        /// Translates `x =&gt; x.Column.Contains(value)`: a `cs` filter for a collection column, or a `like`
        /// filter for a string column.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="item"></param>
        private void VisitColumnContains(Expression collection, Expression item)
        {
            var column = ResolveColumn(collection) ?? throw new ArgumentException($"Left side of expression: '{collection}' is expected to be property with a ColumnAttribute or PrimaryKeyAttribute");
            Filter = collection.Type == typeof(string)
                ? new QueryFilter(column, Operator.Like, "*" + EvaluateExpression(item) + "*")
                : new QueryFilter(column, Operator.Contains, new List<object> { EvaluateExpression(item)! });
        }

        /// <summary>
        /// Translates `x =&gt; capturedCollection.Contains(x.Column)` into an `in` filter, resolving the column
        /// from the argument and evaluating the collection locally.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="item"></param>
        private void VisitCapturedContains(Expression collection, Expression item)
        {
            var column = ResolveColumn(item) ?? throw new ArgumentException($"Argument of expression: '{item}' is expected to be property with a ColumnAttribute or PrimaryKeyAttribute");
            Filter = new QueryFilter(column, Operator.In, EvaluateCollection(collection));
        }

        /// <summary>
        /// Resolves the column name from an expression that references the model parameter, unwrapping a
        /// `Convert` (nullable coercion) or a `Nullable&lt;T&gt;.Value` access along the way.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private string? ResolveColumn(Expression expression) =>
            expression switch
            {
                UnaryExpression { NodeType: ExpressionType.Convert } convert => ResolveColumn(convert.Operand),
                MemberExpression member when IsNullableValueAccess(member) => ResolveColumn(member.Expression!),
                MemberExpression member => GetColumnFromMemberExpression(member),
                _ => null
            };

        /// <summary>
        /// True for the `Nullable&lt;T&gt;.Value` access the compiler inserts around a nullable column (i.e.
        /// the `.Value` in `x.IntValue!.Value`); its inner expression is the column itself.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private static bool IsNullableValueAccess(MemberExpression member) =>
            member.Member.Name == "Value" &&
            member.Expression != null &&
            Nullable.GetUnderlyingType(member.Expression.Type) != null;

        /// <summary>
        /// Evaluates a parameter-free collection expression locally into the list of values expected by an
        /// `in` filter. Conversion wrappers are stripped first so a `T[]` compiled against the span-based
        /// `MemoryExtensions.Contains` (i.e. `op_Implicit(array)` to `ReadOnlySpan&lt;T&gt;`) is evaluated as
        /// the underlying array rather than an un-invokable ref struct.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static IList EvaluateCollection(Expression expression) =>
            EvaluateExpression(UnwrapConversion(expression)) switch
            {
                IList list => list,
                IEnumerable enumerable => enumerable.Cast<object>().ToList(),
                _ => throw new ArgumentException($"Expected the collection in '{expression}' to be enumerable so it can be translated into an `in` filter.")
            };

        /// <summary>
        /// Strips `Convert` and implicit/explicit conversion-operator wrappers to reach the underlying value.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private static Expression UnwrapConversion(Expression expression) =>
            expression switch
            {
                UnaryExpression { NodeType: ExpressionType.Convert } convert => UnwrapConversion(convert.Operand),
                MethodCallExpression { Method: { Name: "op_Implicit" or "op_Explicit" } } call => UnwrapConversion(call.Arguments[0]),
                _ => expression
            };

        /// <summary>
        /// A constant expression parser (i.e. x => x.Id == 5 &lt;- where '5' is the constant)
        /// </summary>
        /// <param name="column"></param>
        /// <param name="op"></param>
        /// <param name="constantExpression"></param>
        private void HandleConstantExpression(string column, Operator op, ConstantExpression constantExpression)
        {
            Filter = BuildFilter(column, op, constantExpression.Value);
        }

        /// <summary>
        /// A member expression parser (i.e. => x.Id == Example.Id &lt;- where both `x.Id` and `Example.Id` are parsed as 'members')
        /// </summary>
        /// <param name="column"></param>
        /// <param name="op"></param>
        /// <param name="memberExpression"></param>
        private void HandleMemberExpression(string column, Operator op, MemberExpression memberExpression)
        {
            Filter = BuildFilter(column, op, GetMemberExpressionValue(memberExpression));
        }

        /// <summary>
        /// Builds a filter from a column, operator and (possibly null) criterion, translating null
        /// equality checks (i.e. `x => x.Name == null`) into the `IS NULL`/`IS NOT NULL` filters
        /// Postgrest expects — at any nesting depth.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="op"></param>
        /// <param name="value"></param>
        private static QueryFilter BuildFilter(string column, Operator op, object? value)
        {
            if (value != null)
                return new QueryFilter(column, op, value);

            return op switch
            {
                Operator.Equals => new QueryFilter(column, Operator.Is, QueryFilter.NullVal),
                Operator.NotEqual => new QueryFilter(column, Operator.Not,
                    new QueryFilter(column, Operator.Is, QueryFilter.NullVal)),
                _ => new QueryFilter(column, op, value)
            };
        }

        /// <summary>
        /// A unary expression parser (i.e. => x.Id == 1 &lt;- where both `1` is considered unary)
        /// </summary>
        /// <param name="column"></param>
        /// <param name="op"></param>
        /// <param name="unaryExpression"></param>
        private void HandleUnaryExpression(string column, Operator op, UnaryExpression unaryExpression)
        {
            if (unaryExpression.Operand is ConstantExpression constantExpression)
            {
                HandleConstantExpression(column, op, constantExpression);
            }
            else if (unaryExpression.Operand is MemberExpression memberExpression)
            {
                HandleMemberExpression(column, op, memberExpression);
            }
            else if (unaryExpression.Operand is NewExpression newExpression)
            {
                HandleNewExpression(column, op, newExpression);
            }
        }

        /// <summary>
        /// An instantiated class parser (i.e. x => x.CreatedAt &lt;= new DateTime(2022, 08, 20) &lt;- where `new DateTime(...)` is an instantiated expression.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="op"></param>
        /// <param name="newExpression"></param>
        private void HandleNewExpression(string column, Operator op, NewExpression newExpression)
        {
            var argumentValues = new List<object>();
            foreach (var argument in newExpression.Arguments)
            {
                var lambda = Expression.Lambda(argument);
                var func = lambda.Compile();
                argumentValues.Add(func.DynamicInvoke());
            }

            var constructor = newExpression.Constructor;
            var instance = constructor.Invoke(argumentValues.ToArray());

            switch (instance)
            {
                case DateTime dateTime:
                    Filter = new QueryFilter(column, op, dateTime);
                    break;
                case DateTimeOffset dateTimeOffset:
                    Filter = new QueryFilter(column, op, dateTimeOffset);
                    break;
                case Guid guid:
                    Filter = new QueryFilter(column, op, guid.ToString());
                    break;
                default:
                {
                    if (instance.GetType().IsEnum)
                    {
                        Filter = new QueryFilter(column, op, instance);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Gets a column name (postgrest) from a Member Expression (used on BaseModel)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string GetColumnFromMemberExpression(MemberExpression node)
        {
            var type = node.Member.ReflectedType;
            var prop = type?.GetProperty(node.Member.Name);
            var attrs = prop?.GetCustomAttributes(true);

            if (attrs == null) return node.Member.Name;

            foreach (var attr in attrs)
            {
                switch (attr)
                {
                    case ColumnAttribute columnAttr:
                        return columnAttr.ColumnName;
                    case PrimaryKeyAttribute primaryKeyAttr:
                        return primaryKeyAttr.ColumnName;
                }
            }

            return node.Member.Name;
        }

        /// <summary>
        /// Get the value from a MemberExpression, which includes both fields and properties.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private object GetMemberExpressionValue(MemberExpression member)
        {
            if (member.Member is FieldInfo field)
            {
                var obj = Expression.Lambda(member.Expression).Compile().DynamicInvoke();
                return field.GetValue(obj);
            }

            var lambda = Expression.Lambda(member);
            var func = lambda.Compile();
            return func.DynamicInvoke();
        }

        /// <summary>
        /// Creates map between linq <see cref="ExpressionType"/> and <see cref="Constants.Operator"/>
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Operator GetMappedOperator(Expression node)
        {
            return node.NodeType switch
            {
                ExpressionType.Not => Operator.Not,
                ExpressionType.And => Operator.And,
                ExpressionType.AndAlso => Operator.And,
                ExpressionType.OrElse => Operator.Or,
                ExpressionType.Or => Operator.Or,
                ExpressionType.Equal => Operator.Equals,
                ExpressionType.NotEqual => Operator.NotEqual,
                ExpressionType.LessThan => Operator.LessThan,
                ExpressionType.GreaterThan => Operator.GreaterThan,
                ExpressionType.LessThanOrEqual => Operator.LessThanOrEqual,
                ExpressionType.GreaterThanOrEqual => Operator.GreaterThanOrEqual,
                _ => Operator.Equals
            };
        }

        /// <summary>
        /// Checks if an expression references the model parameter of the `Where` predicate.
        /// Expressions that don't (i.e. `filterPredicate == null`) can be evaluated locally
        /// instead of being translated into a filter.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private bool ContainsParameter(Expression expression)
        {
            var finder = new ParameterFinder(parameter);
            finder.Visit(expression);
            return finder.Found;
        }

        /// <summary>
        /// Evaluates an expression that doesn't reference the model locally.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private static object? EvaluateExpression(Expression expression) =>
            Expression.Lambda(expression).Compile().DynamicInvoke();

        private class ParameterFinder : ExpressionVisitor
        {
            private readonly ParameterExpression? _target;

            public ParameterFinder(ParameterExpression? target) =>
                _target = target;

            public bool Found { get; private set; }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (_target == null || node == _target)
                    Found = true;

                return base.VisitParameter(node);
            }
        }
    }
}