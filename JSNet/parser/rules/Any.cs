using System.Collections.Generic;

namespace JSNet.parser
{
	sealed class Any<InT> : Rule<InT>
	{
		readonly Rule<InT> anyOf_;

		public Any(Rule<InT> anyOf)
		{
			anyOf_ = anyOf;
		}


		public override Match? doTryMatch(IScope context, Parser<InT>.State state)
		{
			Match m = new Match();
			Match? m1;
			// todo: stop also on 0
			while (null != (m1 = context.tryMatch(anyOf_, state)))
			{
				m += m1.Value;
				state += m1.Value.Count;
			}

			return m;
		}

		public override IEnumerable<Request> doRun(Parser<InT>.State state)
		{
			var res = new Result();

			Match m = new Match();

			for (; ;) // ever
			{
				yield return res.tryMatch(anyOf_, state);

				Match? m1 = res.Match;
				if (m1 == null || m1.Value.Count == 0)
					break;

				m += m1.Value;
				state += m1.Value.Count;
			} 

			yield return m;
		}

		internal override System.Collections.Generic.IEnumerable<Rule<InT>> Dependencies
		{
			get 
			{
				yield return anyOf_;
			}
		}
	}
}
