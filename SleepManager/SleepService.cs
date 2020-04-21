using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;

namespace SleepManager
{
	public partial class SleepService : ServiceBase
	{
		public SleepService()
		{
			InitializeComponent();
		}

		private readonly Settings _settings = new Settings();
		private Timer _timer;
		private Scheduler _scheduler;

		public void StartImpl()
		{
			Close();

			_scheduler = new Scheduler();
			_scheduler.From = _settings.From ?? _scheduler.From;
			_scheduler.ToAccountDisable = _settings.ToAccountDisable ?? _scheduler.ToAccountDisable;
			_scheduler.ToShutdown = _settings.ToShutdown ?? _scheduler.ToShutdown;
			_scheduler.HibernateFirst = _settings.HibernateFirst ?? _scheduler.HibernateFirst;

			_timer = new Timer(Worker);
#if DEBUG
			_timer.Change(0, 1000);
#else
			_timer.Change(0, 5000);
#endif
		}

		protected override void OnStart(string[] args)
		{
			StartImpl();
		}

		private void Worker(object state)
		{
			try
			{
				_scheduler.Tick();
			}
			catch (Exception ex)
			{
				Trace.TraceInformation("Error: " + ex);
			}
		}

		protected override void OnStop()
		{
			Close();
		}

		void Close()
		{
			var timer = _timer;
			if (timer != null)
			{
				timer.Change(Timeout.Infinite, Timeout.Infinite);
				timer.Dispose();
				_timer = null;
			}
			var scheduler = _scheduler;
			if (scheduler != null)
			{
				_scheduler = null;
			}
		}
	}
}
