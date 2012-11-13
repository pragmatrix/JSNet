using System;
using System.IO;

using JSNet;

using System.Diagnostics;

namespace JSRun
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 1)
				throw new Exception("expect one argument, the directory of the javascript files to test");

			string[] files = Directory.GetFiles(args[0], "*.js");

			print("processing directory {0}", args[0]);

			Stopwatch sw = new Stopwatch();
			sw.Start();
			uint successfulFileCount = 0;

			if (files.Length > 1)
			{
				// Array.Resize(ref files, 1);
			}


			foreach (string file in files)
			{
				//string path = Path.Combine(args[0], file);

				print("Input File             : {0}", Path.GetFileName(file));

				try
				{
					using (StreamReader reader = File.OpenText(file))
					{
						string buf = reader.ReadToEnd();

						long ms = sw.ElapsedMilliseconds;

						JSNet.Test.parseJavaScript(buf);

						long elapsed = Math.Max(sw.ElapsedMilliseconds - ms, 1);

						print("Milliseconds           : {0}", elapsed);
						print("Elements per millisec  : {0:n}", (double)buf.Length / elapsed );

					}

					++successfulFileCount;
				}
				catch (Exception e)
				{
					string prefix = "";

					while (e != null)
					{
						print(prefix + e.Message);
						e = e.InnerException;
						prefix += "\t";
					}
				}

				// break; // process only one file for now!
			}

			print("Processed {0} files in {1} milliseconds", successfulFileCount, sw.ElapsedMilliseconds);

		}

		static void print(string str)
		{
			Console.WriteLine(str);
		}

		static void print(string str, params object[] objs)
		{
			Console.WriteLine(str, objs);
		}
	}
}
