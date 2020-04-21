using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SleepManager;

[assembly: TinyInstaller.InstallerIdentity("SleepManager")]
[assembly: TinyInstaller.InstallUserMode(false)]
[assembly: TinyInstaller.InstallUtilsAssembly(typeof(SleepService))]

namespace SleepManagerInstaller
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			TinyInstaller.EntryPoint.GuiRunWith("Sleep Manager");
		}
	}
}
