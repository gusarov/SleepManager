using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SleepManager
{
	public static class Logger
	{
		private static readonly object _lock = new object();

		private static byte _b;

		[Conditional("Never")]
		public static void Write(string str)
		{
			lock (_lock)
			{
				string text = Path.Combine(Path.GetTempPath(), "log.log");
				try
				{
					if (++_b == 8 && new FileInfo(text).Length > 1048576)
					{
						File.Delete(text);
					}
				}
				catch
				{
				}
				try
				{
					File.AppendAllText(text, DateTime.UtcNow.ToString("HHmmss") + " " + Process.GetCurrentProcess().Id + " " + WindowsIdentity.GetCurrent().Name + " " + str + Environment.NewLine);
				}
				catch
				{
				}
			}
		}
	}

}
