/**
	Statistic module.

**/

using System.Diagnostics;

namespace JSNet.parser
{
	sealed partial class Parser<InT>
	{

		internal sealed class Statistics
		{
			readonly uint grammarProductions_;
			readonly ulong inputElements_;

			ulong cacheHits_;
			ulong cacheMisses_;

			public Statistics(ulong inputElements, uint grammarProductions)
			{
				inputElements_ = inputElements;
				grammarProductions_ = grammarProductions;
			}

			[Conditional("DEBUG")]
			public void notifyCacheHit()
			{
				++cacheHits_;
			}

			[Conditional("DEBUG")]
			public void notifyCacheMiss()
			{
				++cacheMisses_;
			}

			public void print()
			{
				{
					System.Console.WriteLine("Grammar productions    : {0}", grammarProductions_);
					System.Console.WriteLine("Input Elements         : {0}", inputElements_);
				}

				ulong evaluations = cacheMisses_ + cacheHits_;
				{
					System.Console.WriteLine("Production evaluations : {0}", evaluations);
					System.Console.WriteLine("Cache Hits             : {0}", cacheHits_);
					System.Console.WriteLine("Cache Misses           : {0}", cacheMisses_);
				}

				if (cacheMisses_ != 0)
					System.Console.WriteLine("Cache Hit Percentage   : {0}%", (uint)((double)cacheHits_ * 100.0 / evaluations));

				{
					System.Console.WriteLine("Evaluations per element: {0:n}", (double)evaluations / inputElements_);
				}

			}
		};
	}
}
