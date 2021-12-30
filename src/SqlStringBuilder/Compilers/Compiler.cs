using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SqlStringBuilder.Common;
using SqlStringBuilder.Interfaces.Common;
using SqlStringBuilder.Interfaces.Select;
using SqlStringBuilder.Internal;
using SqlStringBuilder.Internal.Components;
using SqlStringBuilder.Internal.Constants;
using SqlStringBuilder.Internal.Enums;

namespace SqlStringBuilder.Compilers
{
	/// <summary>
	/// Basic SQL compiler class.
	/// </summary>
	public partial class Compiler
	{
		protected readonly ConditionCompilerReflector _reflector;

		protected string AndIdentifier { get; } = "AND ";

		protected string OrIdentifier { get; } = "OR ";

		protected virtual string SelectAllIdentifier { get; set; } = "*";

		protected virtual string AsIdentifier { get; set; } = "AS";

		protected virtual string OpeningIdentifier { get; set; } = "\"";

		protected virtual string ClosingIdentifier { get; set; } = "\"";

		protected virtual string ParameterPrefix { get; set; } = "@p";

		protected virtual string ParameterPlaceholder { get; set; } = "?";

		protected virtual string EscapeCharacter { get; set; } = "\\";

		public Compiler()
		{
			_reflector = new ConditionCompilerReflector(this);
		}

		public virtual SqlResult Compile(IBaseQueryStatementBuilder builder)
		{
			return builder.QueryType switch
			{
				SqlStatementTypes.Select => CompileSelectStatement((ISelectQueryStatementBuilder)builder),
				SqlStatementTypes.Insert => CompileInsertStatement((IInsertQueryStatementBuilder)builder),
				SqlStatementTypes.Update => CompileUpdateStatement((IUpdateQueryStatementBuilder)builder),
				SqlStatementTypes.Delete => CompileDeleteStatement((IDeleteQueryStatementBuilder)builder),
			};
		}

		internal virtual SqlResult CompileSelectStatement(ISelectQueryStatementBuilder builder)
		{
			// TODO: check IInternalBaseQueryStatementBuilder
			// Неоднозначное решение. Если будет реализацияя билдеров, не имеплементирующая
			// IInternalBaseQueryStatementBuilder, то Compiler не будет работать.
			var ctx = new CompilationContext<ISelectQueryStatementBuilder>(
				(IInternalBaseQueryStatementBuilder<ISelectQueryStatementBuilder>) builder);

			var results = new[]
			{
				CompileSelect(ctx),
				CompileFrom(ctx),
				CompileWheres(ctx),
			}
			.Where(x => x is not null)
			.Where(x => !string.IsNullOrEmpty(x));

			return PrepareResult(ctx, new SqlResult { RawSql = string.Join(" ", results) });
		}

		internal virtual SqlResult CompileInsertStatement(IInsertQueryStatementBuilder builder)
		{
			return new SqlResult();
		}

		internal virtual SqlResult CompileUpdateStatement(IUpdateQueryStatementBuilder builder)
		{
			return new SqlResult();
		}

		internal virtual SqlResult CompileDeleteStatement(IDeleteQueryStatementBuilder builder)
		{
			return new SqlResult();
		}

		internal virtual string CompileSelect<TQuery>(CompilationContext<TQuery> ctx)
			where TQuery : IBaseQueryStatementBuilder
		{
			// TODO: add aggregated columns


			var columns = ctx.Builder
				.GetComponents<AbstractColumn>(ComponentTypes.Select)
				.Select(CompileColumn)
				.ToList();

			var sqlBuilder = new StringBuilder("SELECT ");

			if (((ISelectQueryStatementBuilder)ctx.Builder).IsDistinct)
				sqlBuilder.Append("DISTINCT ");

			string select = columns.Any() ? string.Join(", ", columns) : SelectAllIdentifier;
			sqlBuilder.Append(select);

			return sqlBuilder.ToString();
		}

		internal virtual string CompileTableExpression(AbstractFrom from)
		{
			return Wrap($"{from.Table} {AsIdentifier} {from.Alias}");
		}

		internal virtual string CompileFrom<TQuery>(CompilationContext<TQuery> ctx)
			where TQuery : IBaseQueryStatementBuilder
		{
			if (!ctx.Builder.HasComponent<AbstractFrom>(ComponentTypes.From))
				throw new InvalidOperationException("No table is set");

			var fromComponent = ctx.Builder.GetComponent<AbstractFrom>(ComponentTypes.From);
			return $"FROM {CompileTableExpression(fromComponent)}";
		}

		internal virtual string CompileWheres<TQuery>(CompilationContext<TQuery> ctx)
			where TQuery : IBaseQueryStatementBuilder
		{
			if (!ctx.Builder.HasComponent<AbstractFrom>(ComponentTypes.From) ||
			    !ctx.Builder.HasComponent<AbstractCondition>(ComponentTypes.Where))
				return null;

			var conditions = ctx.Builder.GetComponents<AbstractCondition>(ComponentTypes.Where);
			string sql = CompileConditions(ctx, conditions);

			return string.IsNullOrEmpty(sql) ? null : $"WHERE {sql}";
		}

		internal virtual string CompileColumn(AbstractColumn abstractColumn)
		{
			return Wrap(((Column)abstractColumn).Name);
		}

		/// <summary>
		/// Prepare SQL statement result.
		/// </summary>
		/// <typeparam name="TQuery"></typeparam>
		/// <param name="ctx"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		internal SqlResult PrepareResult<TQuery>(CompilationContext<TQuery> ctx, SqlResult result)
			where TQuery : IBaseQueryStatementBuilder
		{
			result.Bindings = ctx.Builder.Bindings;
			result.Parameters = GenerateNamedParameters(ctx.Builder.Bindings);
			result.Sql = Helper.ReplaceAll(result.RawSql, ParameterPlaceholder, i => ParameterPrefix + i);
			return result;

		}

		/// <summary>
		/// Wrap a single string in a column identifier.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		internal string Wrap(string value)
		{
			string _as = " as ";
			string _point = ".";
			string lower = value.ToLowerInvariant();

			if (lower.Contains(_as))
			{
				var split = lower.Split(_as);
				string before = split[0];
				string after = split[1];
				return Wrap(before) + $" {AsIdentifier} " + WrapValue(after);
			}

			if (lower.Contains(_point))
				return string.Join(_point, value.Split(_point).Select(x => WrapValue(x)));

			// If we reach here then the value does not contain an "AS" alias
			// nor dot "." expression, so wrap it as regular value.
			return WrapValue(value);
		}

		/// <summary>
		/// Wrap a single string in keyword identifiers.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		internal string WrapValue(string value)
		{
			if (value == SelectAllIdentifier)
				return value;

			var opening = OpeningIdentifier;
			var closing = ClosingIdentifier;

			return $"{opening}{value.Replace(closing, closing + closing)}{closing}";
		}

		/// <summary>
		/// Resolve a parameter and add it to the binding list
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="parameter"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		internal virtual string Parameter<TQuery>(CompilationContext<TQuery> ctx, object parameter)
			where TQuery : IBaseQueryStatementBuilder
		{
			ctx.Builder.Bindings.Add(parameter);
			return ParameterPlaceholder;
		}

		protected virtual string CheckOperator(string op)
		{
			if (!Operators.Contains(op))
				throw new InvalidOperationException($"The operator '{op}' cannot be used. Please consider white listing it before using it.");

			return op;
		}

		protected Dictionary<string, object> GenerateNamedParameters(IEnumerable<object> bindings)
			=> bindings.Select((b, i) => new { i, b }).ToDictionary(x => ParameterPrefix + x.i, v => v.b);
	}
}