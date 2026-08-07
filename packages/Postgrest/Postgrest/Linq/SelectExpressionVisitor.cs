using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Supabase.Postgrest.Attributes;

namespace Supabase.Postgrest.Linq

{

	/// <summary>
	/// Helper class for parsing Select linq queries.
	/// </summary>
	internal class SelectExpressionVisitor : ExpressionVisitor
	{
		/// <summary>
		/// The columns that have been selected from this linq expression.
		/// </summary>
		public List<string> Columns { get; } = new();

		/// <summary>
		/// The root call that will be looped through to populate <see cref="Columns"/>.
		/// 
		/// Called like: `Table&lt;Movies&gt;().Select(x => new[] { x.Id, x.Name, x.CreatedAt }).Get()`
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected override Expression VisitNewArray(NewArrayExpression node)
		{
			foreach (var expression in node.Expressions)
				Visit(expression);

			return node;
		}

		/// <summary>
		/// A Member Node, representing a property on a BaseModel.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected override Expression VisitMember(MemberExpression node)
		{
			var column = GetColumnFromMemberExpression(node);

			if (column != null)
				Columns.Add(column);

			return node;
		}

		/// <summary>
		/// A Unary Node, delved into to represent a property on a BaseModel.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected override Expression VisitUnary(UnaryExpression node)
		{
			if (node.Operand is MemberExpression memberExpression)
			{
				var column = GetColumnFromMemberExpression(memberExpression);

				if (column != null)
					Columns.Add(column);
			}

			return node;
		}

		/// <summary>
		/// Gets a column name from property based on it's supplied attributes.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private string? GetColumnFromMemberExpression(MemberExpression node)
		{
			var type = node.Member.ReflectedType;
			var prop = type?.GetProperty(node.Member.Name);
			var attrs = prop?.GetCustomAttributes(true);

			if (attrs == null)
				throw new ArgumentException($"Unknown argument '{node.Member.Name}' provided, does it have a `Column` or `PrimaryKey` attribute?");

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

			throw new ArgumentException($"Unknown argument '{node.Member.Name}' provided, does it have a `Column` or `PrimaryKey` attribute?");
		}
	}
}
