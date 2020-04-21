using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SleepManager.Native;

namespace SleepManager
{
	public class Lsa
	{
		public Settings Settings = new Settings();

		public IEnumerable<string> AllowLogonLocally
		{
			get
			{
				using (var policy = new LSAPolicy())
				{
					return policy.EnumerateAllowLogonLocalSids().ToArray();
				}
			}
		}

		void BackupAccess()
		{
			Trace.TraceInformation("BackupAccess");
			var current = AllowLogonLocally.ToArray();
			if (AccessEnabled && current.Except(Settings.ReservedLogonRight, StringComparer.InvariantCultureIgnoreCase).Except(Settings.OriginalSeInteractiveLogonRight, StringComparer.InvariantCultureIgnoreCase).Any())
			{
				Settings.OriginalSeInteractiveLogonRight = current;
			}
		}

		public bool AccessEnabled
		{
			get
			{
				// if nothing backed up - all returns true, access is enabled
				var currentlyAllowed = AllowLogonLocally.ToArray();
				var originalBackup = Settings.OriginalSeInteractiveLogonRight.ToArray();
				var result = originalBackup.All(currentlyAllowed.Contains);
				return result;
			}
			set
			{
				if (value)
				{
					if (!AccessEnabled)
					{
						Trace.WriteLine("Change AccessEnabled = " + value);
						RestoreAccess();
					}
				}
				else
				{
					if (AccessEnabled)
					{
						Trace.WriteLine("Change AccessEnabled = " + value);
						DisableAccess();
					}
				}
			}
		}

		void DisableAccess()
		{
			Trace.TraceInformation("DisableAccess");
			if (!AccessEnabled)
			{
				Trace.TraceError("Can not disable access - it is already disabled");
				return;
			}
			BackupAccess();
			var value = Settings.OriginalSeInteractiveLogonRight.ToArray();
			if (value.Any())
			{
				using (var policy = new LSAPolicy())
				{
					foreach (var item in value)
					{
						policy.LsaRemoveAccountRights(item);
					}
					var reservedValues = Settings.ReservedLogonRight.ToArray();
					if (reservedValues.Any())
					{
						foreach (var item in reservedValues)
						{
							policy.LsaAddAccountRights(item);
						}
					}
				}
				if (AccessEnabled)
				{
					Trace.TraceError("Failed disabling access: looks like it is still enabled");
				}
			}
			else
			{
				Trace.TraceError("Can not disable access - looks like it is not backed up");
			}
		}

		public void RestoreAccess()
		{
			Trace.TraceInformation("RestoreAccess");
			if (AccessEnabled)
			{
				Trace.TraceError("Can not restore access - it is already enabled");
				return;
			}
			var originalValue = Settings.OriginalSeInteractiveLogonRight.ToArray();
			if (originalValue.Any())
			{
				using (var policy = new LSAPolicy())
				{
					foreach (var item in policy.EnumerateAllowLogonLocalSids().ToArray())
					{
						policy.LsaRemoveAccountRights(item);
					}
					foreach (var item in originalValue)
					{
						policy.LsaAddAccountRights(item);
					}
				}
				if (!AccessEnabled)
				{
					Trace.TraceError("Failed enabling access: looks like it is still disabled");
				}
			}
			else
			{
				Trace.TraceError("Can not restore access: no backup");
			}
		}
	}
}

