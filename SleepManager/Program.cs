using System.ServiceProcess;

namespace SleepManager
{
	internal static class Program
	{
		private static void Main()
		{
			var servicesToRun = new ServiceBase[]
			{
				new SleepService()
			};
			ServiceBase.Run(servicesToRun);
		}
	}
}
