using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;

namespace SleepManager
{
	[RunInstaller(true)]
	public partial class ServerInstaller : Installer
	{
		public ServerInstaller()
		{
			InitializeComponent();
			// TinyInstaller.EntryPoint .EnsureDebugger();
		}

		ServiceController Service
		{
			get
			{
				return ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == serviceInstaller1.ServiceName);
			}
		}

		protected override void OnAfterInstall(IDictionary savedState)
		{
			try
			{
				base.OnAfterInstall(savedState);
				var service = Service;
				if (service != null)
				{
					service.Start();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceWarning(ex.ToString());
			}
		}

		protected override void OnBeforeUninstall(IDictionary savedState)
		{
			try
			{
				base.OnBeforeUninstall(savedState);
				var service = Service;
				if (service != null && service.CanStop)
				{
					service.Stop();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceWarning(ex.ToString());
			}
		}
	}
}
