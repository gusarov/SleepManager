using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using SleepManager;

namespace ConsoleApplication1
{
	class Program
	{
		[DllImport("user32")]
		static extern bool LockWorkStation();

		static void Main(string[] args)
		{
			Console.WriteLine(new Lsa().AccessEnabled);
			return;

			Trace.Listeners.Add(new ConsoleTraceListener());

			var lsa = new Lsa();

			Console.WriteLine("AccessEnabled = " + lsa.AccessEnabled);
			foreach (var user in lsa.AllowLogonLocally)
			{
				Console.Write(user);
				Console.Write(" = ");
				try
				{
					Console.WriteLine(new SecurityIdentifier(user).Translate(typeof(NTAccount)));
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.GetType().Name);
				}
			}
			Console.WriteLine("-end-");

			if (args.Length == 0)
			{
				lsa.AccessEnabled = true;
			}
			else
			{
				lsa.AccessEnabled = false;
			}
		}
	}
}
