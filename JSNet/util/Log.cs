
using System.Diagnostics;
using System.Linq;
using System;
namespace JSNet
{
	static class Log
	{
		[Conditional("DEBUG")]
		public static void D(string message, params object[] values)
		{
			log("D ", message, values);
		}

		[Conditional("TRACE")]
		public static void T(string message, params object[] values)
		{
			log("T ", message, values);
		}

		static void log(string prefix, string message, params object[] values)
		{
			string r;
			try
			{
				if (values.Length != 0)
					r = string.Format(message, values);
				else
					r = message;
			}
			catch (Exception)
			{
				// string format may fail, because we include unexpected input!
				r = message + ": " + string.Join(", ", (from v in values select v.ToString()).ToArray());
			}
	
			Debug.WriteLine(prefix + r);
		}
	}
}
