using System.Collections.Generic;
using System;
using System.Diagnostics;

using JSNet.util;

namespace JSNet.parser
{
	abstract class Terminal<InT> : Rule<InT>
	{
		public static implicit operator Terminal<InT>(InT element)
		{
			return new ElementTerminal<InT>(element);
		}

		public static implicit operator Terminal<InT>(string terminal)
		{
			Debug.Assert(terminal.Length != 0);
			Debug.Assert(typeof(InT) == typeof(char));


			InT[] chars = new InT[terminal.Length];
			terminal.CopyTo(0, chars as char[], 0, terminal.Length);

			if (terminal.Length == 1)
				return new ElementTerminal<InT>(chars[0]);

			return new SequenceTerminal<InT>(chars);
		}

		public static implicit operator Terminal<InT>(MatchFunctor<InT> functor)
		{
			return new FunctionTerminal<InT>(functor);
		}

		// a terminal cannot have any rule dependencies

		internal override sealed IEnumerable<Rule<InT>> Dependencies
		{
			get 
			{
				yield break;
			}
		}
	}

	/**
		Empty
	**/

	sealed class Empty<InT> : Terminal<InT>
	{
		public Empty()
		{ }

		public override Match? doTryMatch(IScope context, Parser<InT>.State state)
		{
			return new Match(0);
		}

		public override IEnumerable<Request> doRun(Parser<InT>.State state)
		{
			yield return new Request(0);
		}

		public override string ToString()
		{
			return "[empty]";
		}

	}


	sealed class ElementTerminal<InT> : Terminal<InT>
	{
		readonly InT element_;

		public ElementTerminal(InT element)
		{
			element_ = element;
		}

		public override Match? doTryMatch(IScope context, Parser<InT>.State state)
		{
			if (!state.available(1))
				return null;

			if (!element_.Equals(state[0]))
				return null;

			return new Match(1);
		}

		public override IEnumerable<Request> doRun(Parser<InT>.State state)
		{
			if (state.available(1) && element_.Equals(state[0]))
				yield return new Match(1);
			else
				yield return null;
		}

		public override string ToString()
		{
			return element_.ToString();
		}


	}

	sealed class SequenceTerminal<InT> : Terminal<InT>
	{
		readonly InT[] sequence_;

		public SequenceTerminal(InT[] sequence)
		{
			sequence_ = sequence;
		}

		public override Match? doTryMatch(IScope context, Parser<InT>.State state)
		{
			if (!state.available(sequence_.Length))
				return null;

			for (uint i = 0; i != sequence_.Length; ++i)
				if (!sequence_[i].Equals(state[i]))
					return null;

			return new Match((uint)sequence_.Length);
		}

		public override IEnumerable<Request> doRun(Parser<InT>.State state)
		{
			throw new NotImplementedException();
		}

		public override string ToString()
		{
			return
				"\"" +
					string.Join(string.Empty,
					Array.ConvertAll<InT, string>(sequence_, delegate(InT e) { return e.ToString(); }))
					+ "\"";
		}

	}



	delegate int MatchFunctor<InT>(Parser<InT>.State state);

	sealed class FunctionTerminal<InT> : Terminal<InT>
	{
		readonly MatchFunctor<InT> functor_;

		public FunctionTerminal(MatchFunctor<InT> functor)
		{
			functor_ = functor;
		}

		public override Match? doTryMatch(IScope context, Parser<InT>.State state)
		{
			uint matched = (uint)functor_(state);
			if (matched == 0)
				return null;

			return new Match(matched);
		}

		public override IEnumerable<Request> doRun(Parser<InT>.State state)
		{
			throw new NotImplementedException();
		}


		public override string ToString()
		{
			return "function";
		}
	}

}
