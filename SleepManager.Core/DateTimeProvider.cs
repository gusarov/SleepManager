using System.Linq;
using System.Collections.Generic;
using System;

namespace SleepManager
{
	public class DateTimeProvider : IDateTimeProvider
	{
		public DateTime UtcNow
		{
			get { return DateTime.UtcNow; }
		}
	}
}