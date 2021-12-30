using System;
using System.Linq;
using System.Text;

namespace SqlStringBuilder.Internal
{
	internal static class Helper
	{
		public static string ReplaceAll(string subject,
			string match, Func<int, string> callback)
		{
			if (string.IsNullOrEmpty(subject) || !subject.Contains(match))
				return subject;

			var split = subject.Split(match);

			return split.Skip(1)
				.Select((x, i) => callback(i) + x)
				.Aggregate(new StringBuilder(split.First()), (accumulator, current) => accumulator.Append(current))
				.ToString();
		}
	}
}
