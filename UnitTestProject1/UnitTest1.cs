using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using SleepManager;

namespace UnitTestProject1
{
	[TestClass]
	public class UnitTest1
	{


		[TestMethod]
		public void ShouldAccessWmi()
		{
			unsafe
			{
				Assert.AreEqual(4, sizeof(IntPtr));
			}

			foreach (var user in new Lsa().AllowLogonLocally)
			{
				Console.WriteLine(user);
			}

			new Lsa().RestoreAccess();
		}

		[TestMethod]
		public void ShouldTrackAccess()
		{
			var core = new Lsa();
			core.AccessEnabled = true;
			Assert.IsTrue(core.AccessEnabled);
			core.AccessEnabled = false;
			Assert.IsFalse(core.AccessEnabled);
			core.AccessEnabled = true;
			Assert.IsTrue(core.AccessEnabled);
		}

		private Scheduler Prepare(string from, string toA, string toS, string date)
		{
			var ci = CultureInfo.InvariantCulture;
			return Prepare(TimeSpan.Parse(from, ci), TimeSpan.Parse(toA, ci), TimeSpan.Parse(toS, ci), DateTime.ParseExact(date, "yyyy-M-d HH:mm:ss", ci));
		}

		Scheduler Prepare(TimeSpan from, TimeSpan toA, TimeSpan toS, DateTime date)
		{
			var sut = new Scheduler()
			{
				DateTimeProvider = new FakeDateTimeProvider
				{
					UtcNow = date.ToUniversalTime(),
				},
				From = from,
				ToAccountDisable = toA,
				ToShutdown = toS,
			};
			return sut;
		}

		[TestMethod]
		public void Should_01_AllowedTime()
		{
			Assert.IsTrue(Prepare("8:00:00", "22:00:00", "23:00:00", "2000-1-1 10:00:00").IsAllowedTime(Scheduler.SpanKind.Shutdown));
			Assert.IsTrue(Prepare("8:00:00", "22:00:00", "23:00:00", "2000-1-1 10:00:00").IsAllowedTime(Scheduler.SpanKind.Account));
		}

		[TestMethod]
		public void Should_02_AllowedTime()
		{
			Assert.IsFalse(Prepare("8:00:00", "22:00:00", "23:00:00", "2000-1-1 22:20:00").IsAllowedTime(Scheduler.SpanKind.Account));
			Assert.IsTrue(Prepare("8:00:00", "22:00:00", "23:00:00", "2000-1-1 22:20:00").IsAllowedTime(Scheduler.SpanKind.Shutdown));

			Assert.IsFalse(Prepare("8:00:00", "22:00:00", "23:00:00", "2000-1-1 23:40:00").IsAllowedTime(Scheduler.SpanKind.Shutdown));
			Assert.IsFalse(Prepare("8:00:00", "22:00:00", "23:00:00", "2000-1-1 07:40:00").IsAllowedTime(Scheduler.SpanKind.Shutdown));
		}

		[TestMethod]
		public void Should_02_AllowedCrossZeroTime()
		{
			Assert.IsTrue(Prepare("8:00:00", "1:00:00", "2:00:00", "2000-1-1 10:40:00").IsAllowedTime(Scheduler.SpanKind.Shutdown));
			Assert.IsTrue(Prepare("8:00:00", "1:00:00", "2:00:00", "2000-1-1 00:40:00").IsAllowedTime(Scheduler.SpanKind.Shutdown));
			Assert.IsFalse(Prepare("8:00:00", "1:00:00", "2:00:00", "2000-1-1 03:40:00").IsAllowedTime(Scheduler.SpanKind.Shutdown));

			Assert.IsFalse(Prepare("6:00:00", "0:00:00", "0:20:00", "2000-1-1 03:40:00").IsAllowedTime(Scheduler.SpanKind.Shutdown));
			Assert.IsFalse(Prepare("6:00:00", "0:00:00", "0:20:00", "2000-1-1 00:21:00").IsAllowedTime(Scheduler.SpanKind.Shutdown));
			Assert.IsTrue(Prepare("6:00:00", "0:00:00", "0:20:00", "2000-1-1 00:19:00").IsAllowedTime(Scheduler.SpanKind.Shutdown));

			Assert.IsTrue(Prepare("6:00:00", "0:00:00", "0:20:00", "2000-1-1 23:59:00").IsAllowedTime(Scheduler.SpanKind.Account));
			Assert.IsTrue(Prepare("6:00:00", "0:00:00", "0:20:00", "2000-1-2 00:00:00").IsAllowedTime(Scheduler.SpanKind.Account));
			Assert.IsFalse(Prepare("6:00:00", "0:00:00", "0:20:00", "2000-1-2 00:00:01").IsAllowedTime(Scheduler.SpanKind.Account));
		}

		private const string _registryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\SleepManager";

		[TestMethod]
		public void ReadDwordSetting()
		{
			var q1 = Registry.GetValue(_registryKey, "HibernateFirst_", 0);
			var q2 = Registry.GetValue(_registryKey, "HibernateFirst", null);
			var q3 = Registry.GetValue(_registryKey, "HibernateFirst", 0);
			Assert.AreEqual(typeof(int), q1.GetType());
			Assert.AreEqual(typeof(int), q2.GetType());
			Assert.AreEqual(typeof(int), q3.GetType());
		}

		[TestMethod]
		public void ReadDwordSettingViaClass()
		{
			var s = new Settings();
			Assert.AreEqual(null, s.HibernateFirst);
		}
	}

	public class FakeDateTimeProvider : IDateTimeProvider
	{
		public DateTime UtcNow { get; set; }
	}
}
