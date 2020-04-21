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
				if (UpTime.TotalMinutes < 10)
				{
					Trace.TraceInformation("UpTime.TotalMinutes < 10 - ForceShutdown");
					ForceShutdown();
				}
				else
				{
					var now = DateTimeProvider.UtcNow;
					if (_shutdownAttempted.HasValue
					    && (now - _shutdownAttempted) >= TimeSpan.FromMinutes(3))
					{
						Trace.TraceInformation(">= 3min - ForceShutdown");
						ForceShutdown();
					}
					else if (_shutdownAttempted.HasValue
					         && (now - _shutdownAttempted) >= TimeSpan.FromMinutes(2))
					{
						Trace.TraceInformation(">= 2min - ForceHibernate");
						ForceHibernate();
					}
					else if (_shutdownAttempted.HasValue
					      && (now - _shutdownAttempted) >= TimeSpan.FromMinutes(35))
					{
						if (HibernateFirst)
						{
							Trace.TraceInformation(">= 35sec - Hibernate instead");
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
						GracefulShutdown();
					}
				}
			}
			else
			{
				_shutdownAttempted = null; // process survives after hibernate
			}
		}

		private void GracefulShutdown()
		{
			try
			{
				Process.Start("shutdown", "/s /t 45"); // no force and 30 sec notification
			}
			catch
			{
			}
		}

		private void ShutdownAbort()
		{
			try
			{
				Process.Start("shutdown", "/a"); // no force and 30 sec notification
			}
			catch
			{
			}
		}

		private readonly Lsa _lsa = new Lsa();

		private bool LogonEnabled
		{
			get { return _lsa.AccessEnabled; }
			set { _lsa.AccessEnabled = value; }
		}

		private static void ForceHibernate()
		{
			try
			{
				Process.Start("shutdown", "/h /f");
			}
			catch
			{
			}
		}

		private static void Hibernate()
		{
			try
			{
				Process.Start("shutdown", "/h");
			}
			catch
			{
			}
		}

		private static void ForceShutdown()
		{
			try
			{
				Process.Start("shutdown", "/s /f /t 0");
			}
			catch
			{
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