using System;
using System.Collections.Generic;
using System.Linq;

using SqlStringBuilder.Interfaces.Common;
using SqlStringBuilder.Internal;
using SqlStringBuilder.Internal.Components;
using SqlStringBuilder.Internal.Constants;

namespace SqlStringBuilder.Compilers
{
	/// <summary>
	/// Basic SQL compiler class.
	/// </summary>
	public partial class Compiler
	{
		/// <summary>
		/// A list of white-listed operators
		/// </summary>
		/// <value></value>
		protected readonly HashSet<string> Operators =
			new(ComparisonOperators.ToList().Concat(DbComparisonOperators.ToList()));

		internal virtual string CompileConditions<TQuery>(CompilationContext<TQuery> ctx,
			List<AbstractCondition> conditions)
			where TQuery : IBaseQueryStatementBuilder
		{
			var result = new List<string>(conditions.Count);

			for (int i = 0; i < conditions.Count; i++)
			{
				string compiled = CompileCondition(ctx, conditions[i]);
				if (string.IsNullOrEmpty(compiled))
					continue;

				string boolOperator = i == 0 ? string.Empty : (conditions[i].IsOr ? OrIdentifier : AndIdentifier);
				result.Add(boolOperator + compiled);
			}

			return string.Join(" ", result);
		}

		internal virtual string CompileCondition<TQuery>(CompilationContext<TQuery> ctx,
			AbstractCondition condition)
			where TQuery : IBaseQueryStatementBuilder
		{
			var name = condition.GetType().Name;
			name = name.Substring(0, name.IndexOf("Condition"));
			string methodName = $"Compile{name}Condition";

			var methodInfo = _reflector.GetMethodInfo(condition.GetType(), methodName, typeof(CompilationContext<TQuery>));

			try
			{
				string sql = (string)methodInfo.Invoke(this, new object[]
				{
					ctx,
					condition,
				});

				return sql;

			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to invoke '{methodName}'", ex);
			}
		}

		internal virtual string CompileBasicCondition<TQuery, TValue>(CompilationContext<TQuery> ctx,
			BasicCondition<TValue> condition)
			where TQuery : IBaseQueryStatementBuilder
		{
			var sql = $"{Wrap(condition.Column)} {CheckOperator(condition.Operator)} {Parameter(ctx, condition.Value)}";

			if (condition.IsNot)
				sql = $"NOT ({sql})";

			return sql;
		}

		internal virtual string CompileNullCondition<TQuery>(CompilationContext<TQuery> ctx,
			NullCondition condition)
			where TQuery : IBaseQueryStatementBuilder
		{
			var op = condition.IsNot ? "IS NOT NULL" : "IS NULL";
			return Wrap(condition.Column) + " " + op;
		}
	}
}