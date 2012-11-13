/**
	Sequence
**/

using System.Collections.Generic;
using JSNet.util;
using System;

namespace JSNet.parser
{
	sealed class Sequence<InT> : Rule<InT>
	{
		readonly Rule<InT> first_;
		readonly Rule<InT> second_;

		public Sequence(Rule<InT> first, Rule<InT> second)
		{
			first_ = first;
			second_ = second;
		}

		public override Match? doTryMatch(IScope context, Parser<InT>.State state)
		{
			Match? first = context.tryMatch(first_, state);
			if (first == null)
				return null;

			state += first;

			Match? second = context.tryMatch(second_, state);
			if (second == null)
				return null;

			return first + second;
		}

		public override IEnumerable<Request> doRun(Parser<InT>.State state)
		{
			var future = new Result();

			yield return future.tryMatch(first_, state);
			if (future.Match == null)
			{
				yield return null;
				yield break;
			}

			Match firstMatch = future.Match.Value;
			state += firstMatch;

			yield return future.tryMatch(second_, state);
			if (future.Match == null)
			{
				yield return null;
				yield break;
			}

			yield return firstMatch + future.Match;
		}


		internal override IEnumerable<Rule<InT>> Dependencies
		{
			get 
			{
				yield return first_;
				yield return second_;
			}
		}

		public override string ToString()
		{
			return string.Format("{0}+{1}", first_, second_);
		}

	}
}
