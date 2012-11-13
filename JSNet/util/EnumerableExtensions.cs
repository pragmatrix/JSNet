using System.Collections.Generic;
using System;

using System.Linq;

namespace JSNet.util
{
	static class EnumerableExtensions
	{
		public static void apply<T>(this IEnumerable<T> enumerable, Action<T> a)
		{
			foreach (T t in enumerable)
				a(t);
		}
	}
}
