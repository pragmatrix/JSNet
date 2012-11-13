/**
	The rule scope is used call the evaluation method of the production that
	has generated the scope.
**/

namespace JSNet.parser
{
	abstract partial class Rule<InT>
	{
		internal interface IScope
		{
			Match? tryMatch(Rule<InT> rule, Parser<InT>.State state);
		}
	}
}
