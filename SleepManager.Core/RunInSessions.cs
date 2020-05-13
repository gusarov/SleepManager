using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SleepManager
{
	class RunInSessions
	{
		public void Run()
		{
			try
			{
				Logger.Write("Run in sessions... " + Assembly.GetEntryAssembly().Location);
				foreach (var session in Advapi32.GetSessions())
				{
					try
					{
						var startupInfo = default(Advapi32.STARTUPINFO);
						startupInfo.cb = Marshal.SizeOf((object)startupInfo);
						Logger.Write(session.ToString());
						Advapi32.WTSQueryUserToken((uint)session.Item1, out var token);
						Logger.Write(token.ToString());
						var sa = default(Advapi32.SECURITY_ATTRIBUTES);
						sa.Length = Marshal.SizeOf((object)sa);
						var lpEnvironment = IntPtr.Zero;
						Advapi32.CreateEnvironmentBlock(out lpEnvironment, token, bInherit: false);
						var processAttributes = default(Advapi32.SECURITY_ATTRIBUTES);
						var threadAttributes = default(Advapi32.SECURITY_ATTRIBUTES);
						var r = Advapi32.CreateProcessAsUser(token, null, $"\"{Assembly.GetEntryAssembly().Location}\" /inses",
							ref processAttributes, ref threadAttributes, inheritHandles: false, 134218784u,
							lpEnvironment, null, ref startupInfo, out _);
						Logger.Write(r.ToString() + " " + Marshal.GetLastWin32Error());
					}
					catch (Exception ex)
					{
						Logger.Write(ex.ToString());
					}
				}
			}
			catch (Exception ex2)
			{
				Logger.Write(ex2.ToString());
			}
		}
	}
}
