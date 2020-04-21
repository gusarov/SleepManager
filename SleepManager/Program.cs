using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;

namespace SleepManager
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			var service = new SleepService();
			if (args.Any())
			{
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
