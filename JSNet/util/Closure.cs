
using System.Collections.Generic;

namespace JSNet.util
{
	static class Closure
	{
		/**
			Build a closure.
		**/

		public delegate IEnumerable<T> DependenciesOf<T>(T v);

		public static IEnumerable<T> build<T>(T root, DependenciesOf<T> dependenciesOf)
		{
			Dictionary<T, Null> table_ = new Dictionary<T, Null>();
			table_.Add(root, Null.Value);

			Queue<T> todo = new Queue<T>();
			todo.Enqueue(root);

			while (todo.Count != 0)
			{
				T current = todo.Dequeue();
				yield return current;

				foreach (T dep in dependenciesOf(current))
				{
					Null tmp;

					if (!table_.TryGetValue(dep, out tmp))
					{
						todo.Enqueue(dep);
						table_[dep] = Null.Value;
					}
				}
			}
		}

		struct Null 
		{
			public static Null Value = new Null();
		};
	}
}
