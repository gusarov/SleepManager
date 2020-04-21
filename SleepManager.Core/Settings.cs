using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.Win32;

namespace SleepManager
{
	public class Settings
	{
		private const string _registryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\SleepManager";
		private const string OriginalSeInteractiveLogonRightProperty = "OriginalSeInteractiveLogonRight";
		private const string ReservedLogonRightProperty = "ReservedLogonRight";
		private const string FromProperty = "From";
		private const string ToShutdownProperty = "ToShutdown";
		private const string ToAccountDisableProperty = "ToAccountDisable";
		private const string HibernateFirstProperty = "HibernateFirst";

		protected virtual string this[string property]
		{
			get { return Registry.GetValue(_registryKey, property, "") as string; }
			set { Registry.SetValue(_registryKey, property, value); }
		}

		public IEnumerable<string> OriginalSeInteractiveLogonRight
		{
			get { return (this[OriginalSeInteractiveLogonRightProperty] ?? "").Split(new []{';'},StringSplitOptions.RemoveEmptyEntries); }
			set { this[OriginalSeInteractiveLogonRightProperty] = string.Join(";", value); }
		}

		public IEnumerable<string> ReservedLogonRight
		{
			get { return (this[ReservedLogonRightProperty] ?? "").Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries); }
			set { this[ReservedLogonRightProperty] = string.Join(";", value); }
		}

		TimeSpan? Try(string value)
		{
			TimeSpan result;
			if (TimeSpan.TryParse(value, out result))
			{
				return result;
			}
			return null;
		}

		public bool? HibernateFirst
		{
			get
			{
				var v = (int?)Registry.GetValue(_registryKey, HibernateFirstProperty, null);
				if (v.HasValue)
				{
					return v.Value != 0;
				}
				else
				{
					return null;
				}
			}
			set
			{
				if (value.HasValue)
				{
					Registry.SetValue(_registryKey, HibernateFirstProperty, value.Value ? 1 : 0);
				}
			}
		}

		public TimeSpan? From
		{
			get { return Try(this[FromProperty]); }
			set { this[FromProperty] = value.ToString(); }
		}

		public TimeSpan? ToShutdown
		{
			get { return Try(this[ToShutdownProperty]); }
			set { this[ToShutdownProperty] = value.ToString(); }
		}

		public TimeSpan? ToAccountDisable
		{
			get { return Try(this[ToAccountDisableProperty]); }
			set { this[ToAccountDisableProperty] = value.ToString(); }
		}
	}
}