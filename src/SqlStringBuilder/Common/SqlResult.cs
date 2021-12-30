using System.Collections.Generic;

namespace SqlStringBuilder.Common
{
	/// <summary>
	/// Represents compile result of SQL components.
	/// </summary>
	public class SqlResult
	{
		/// <summary>
		/// Raw SQL without replaced parameter names.
		/// </summary>
		public string RawSql { get; set; }

		/// <summary>
		/// Result SQL.
		/// </summary>
		public string Sql { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public List<object> Bindings { get; set; } = new ();

		public Dictionary<string, object> Parameters { get; set; } = new ();
	}
}