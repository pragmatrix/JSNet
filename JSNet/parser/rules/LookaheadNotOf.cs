using System.Collections.Generic;

using JSNet.util;
using System;

namespace JSNet.parser
{
	/**
		LookaheadNotOf

		Succeed with 0 characters match if notOf_ is not matched.
	**/

	sealed class LookaheadNotOf<InT> : Rule<InT>
	{
		readonly Rule<InT> notOf_;

		public LookaheadNotOf(Rule<InT> notOf)
		{
			notOf_ = notOf;
		}

		public override Match? doTryMatch(IScope context, Parser<InT>.State state)
		{
			if (null == context.tryMatch(notOf_, state) && !state.Control.FailAllRules)
				return new Match(0);

			return null;
		}

		public override IEnumerable<Request> doRun(Parser<InT>.State state)
		{
			var res = new Result();

			yield return new Request(notOf_, state, res);
			if (res.Match == null)
				yield return new Match(0);
			else
				yield return null;
		}


		internal override IEnumerable<Rule<InT>> Dependencies
		{
			get
			{
				yield return notOf_;
			}
		}
	}
}
