/**


**/

namespace JSNet.parser
{
	sealed partial class Parser<InT>
	{
		public sealed class Exception : System.Exception
		{
			readonly State state_;

			internal Exception(State state, string description)
				: base(state + ": " + description)
			{
				state_ = state;
			}

			public uint Offset
			{
				get
				{
					return state_.Offset;
				}
			}
		}
	}
}
