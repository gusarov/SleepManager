using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System;

namespace SleepManager
{
	public class Scheduler
	{
		public IDateTimeProvider DateTimeProvider = new DateTimeProvider();

		public TimeSpan From  = new TimeSpan(6, 0, 0);
		public TimeSpan ToAccountDisable  = new TimeSpan(22, 30, 0);
		public TimeSpan ToShutdown  = new TimeSpan(23, 30, 0);
		public bool HibernateFirst = true;

		public enum SpanKind
		{
			Unknown,
			Account,
			Shutdown,
		}

		public bool IsAllowedTime(SpanKind kind)
		{
			switch (kind)
			{
				case SpanKind.Unknown:
					throw new NotSupportedException();
				case SpanKind.Account:
					return IsInRange(From, ToAccountDisable);
				case SpanKind.Shutdown:
					return IsInRange(From, ToShutdown);
				default:
					throw new ArgumentOutOfRangeException("kind");
			}
		}

		private static TimeSpan Max(TimeSpan a, TimeSpan b)
		{
			return a < b ? b : a;
		}

		private static TimeSpan Min(TimeSpan a, TimeSpan b)
		{
			return a > b ? b : a;
		}

		private bool IsInRange(TimeSpan from, TimeSpan to)
		{
			var min = Min(from, to);
			var max = Max(from, to);
			var now = DateTimeProvider.UtcNow.ToLocalTime();
			var nowTime = now - now.Date;
			var r = (min < nowTime && nowTime < max);
			if (from > to) // cross day span
			{
				r = !r;
			}

			// Trace.WriteLine(string.Format("Now {0} in range {1} and {2} is {3}", now, ));
			return r;
		}

		public void Tick()
		{
			// block access, e.g. when not in 6:00 - 22:30
			SetLogonAccess();

			// shutdown, e.g. when not  6:00 - 23:30
			ShutdownIfBadTime();
		}

		private void SetLogonAccess()
		{
			LogonEnabled = IsAllowedTime(SpanKind.Account);
		}

		private void ShutdownIfBadTime()
		{
			if (!IsAllowedTime(SpanKind.Shutdown))
			{
				Trace.TraceInformation("!IsAllowedTime");
				var now = DateTimeProvider.UtcNow;

				var timeInFight = now - _shutdownAttempted;

				if (UpTime.TotalMinutes <
#if DEBUG
					1
#else
					10
#endif
					) // top priority - if you are booting again (from zero) in restricted time - shutdown immediately
				{
					Trace.TraceInformation("UpTime.TotalMinutes < 10 - ForceShutdown");
					ForceShutdown();
				}
				else if (timeInFight >= TimeSpan.FromMinutes(3))
				{
					Trace.TraceInformation(">= 3min - ForceShutdown");
					ForceShutdown();
				}
				else if (timeInFight >= TimeSpan.FromMinutes(2))
				{
					Trace.TraceInformation(">= 2min - ForceHibernate");
					ForceHibernate();
				}
				else if (timeInFight >= TimeSpan.FromSeconds(45))
				{
					if (HibernateFirst)
					{
						Trace.TraceInformation(">= 45sec - Hibernate instead");
						ShutdownAbort();
						Hibernate();
					}
				}
				else
				{
					Trace.TraceInformation("GracefulShutdown");
					if (!_shutdownAttempted.HasValue)
					{
						_shutdownAttempted = now;
					}

					GracefulShutdown(); // actually it will just show a warning for N minute. If hibernate enabled - it will abort, and hibernate instead
				}

			}
			else
			{
				if (_shutdownAttempted.HasValue)
				{
					_shutdownAttempted = null; // process survives after hibernate and now in proper state
					ShutdownAbort(); // just in case if been scheduled
				}
			}
		}

		private void GracefulShutdown()
		{
			var sgable = Environment.OSVersion.Version >= new Version("10.0.0.0");

			var g = sgable ? "g" : "";
			var graceful = Run("shutdown", $"/s{g} /t 59"); // at least Vista
			if (graceful != 0) // denied by restart manager
			{
				Run("shutdown", "/s /t 59"); // without g
			}
			/*
			try
			{
				Trace.TraceInformation("shutdown /sg /t 59");

				var p = Process.Start("shutdown", "/sg /t 59");
				p.WaitForExit();
				Trace.TraceInformation("code: " + p.ExitCode);
				if (p.ExitCode != 0)
				{
					throw new Exception();
				}
			}
			catch
			{
				Trace.TraceInformation("Exception");
				Trace.TraceInformation("shutdown /s /t 59");

				try
				{
					var p = Process.Start("shutdown", "/s /t 59");
					p.WaitForExit();
					Trace.TraceInformation("code: " + p.ExitCode);
				}
				catch
				{
				}
			}
			*/
		}

		private void ShutdownAbort()
		{
			Run("shutdown", "/a");
			/*
			try
			{
				Trace.TraceInformation("shutdown /a");
				var p = Process.Start("shutdown", "/a");
				p.WaitForExit();
				Trace.TraceInformation("code: " + p.ExitCode);
			}
			catch
			{
			}
			*/
		}

		private readonly Lsa _lsa = new Lsa();

		private bool LogonEnabled
		{
			get { return _lsa.AccessEnabled; }
			set { _lsa.AccessEnabled = value; }
		}

		private static void ForceHibernate()
		{
			Run("powercfg", "/h on");
			Run("shutdown", "/h /f");
			/*
			try
			{
				Trace.TraceInformation("shutdown /h /f");

				var p = Process.Start("shutdown", "/h /f");
				p.WaitForExit();
				Trace.TraceInformation("code: " + p.ExitCode);

			}
			catch
			{
			}
			*/
		}

		private static void Hibernate()
		{
			Run("powercfg", "/h on");
			Run("shutdown", "/h");
			/*
			try
			{
				Trace.TraceInformation("shutdown /h");

				var p = Process.Start("shutdown", "/h");
				p.WaitForExit();
				Trace.TraceInformation("code: " + p.ExitCode);
			}
			catch
			{
			}
			*/
		}

		private static void ForceShutdown()
		{
			Run("shutdown", "/s /f /t 0");
			/*
			try
			{
				Trace.TraceInformation("shutdown /s /f /t 0");

				var p =Process.Start("shutdown", "/s /f /t 0");
				p.WaitForExit();
				Trace.TraceInformation("code: " + p.ExitCode);
			}
			catch
			{
			}
			*/
		}

		static int Run(string cmd, string args)
		{
			try
			{
				Trace.TraceInformation($"{cmd} {args}");

				var psi = new ProcessStartInfo(cmd, args)
				{
					// RedirectStandardOutput = true,
					// RedirectStandardError = true,
					UseShellExecute = false,
				};
				var p = Process.Start(psi);
				p.WaitForExit();
				Trace.TraceInformation("code: " + p.ExitCode);
				return p.ExitCode;
			}
			catch
			{
				return -1;
			}
		}


		private bool _tryAutoLodCtr;

		private DateTime? _shutdownAttempted;

		public TimeSpan UpTime
		{
			get
			{
				try
				{
					using (var uptime = new PerformanceCounter("System", "System Up Time"))
					{
						uptime.NextValue(); // Call this an extra time before reading its value
						return TimeSpan.FromSeconds(uptime.NextValue());
					}
				}
				catch
				{
					if (!_tryAutoLodCtr)
					{
						_tryAutoLodCtr = true;
						try
						{
							// fix perf counters cache automatically
							Process.Start("lodctr", "/r");
						}
						catch
						{

						}
					}

					return TimeSpan.FromDays(1); // hardcoded
				}
			}
		}

	}
}