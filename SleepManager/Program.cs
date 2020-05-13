using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace SleepManager
{
	internal static class Program
	{
		[DllImport("user32")]
		static extern bool LockWorkStation();

		private static void Main(string[] args)
		{
			Logger.Write("Started: " + string.Join("|", args));
			var service = new SleepService();
			if (args.Any())
			{
				if (string.Equals(args[0], "/inses", StringComparison.OrdinalIgnoreCase))
				{
					Logger.Write("Lock");
					LockWorkStation();
					return;
				}

				Trace.Listeners.Add(new ConsoleTraceListener());

				service.StartImpl();
				Console.WriteLine("Press <enter> to stop. . .");
				Console.ReadLine();
			}
			else
			{
				var servicesToRun = new ServiceBase[]
				{
					service
				};
				ServiceBase.Run(servicesToRun);
			}
		}
	}
}
