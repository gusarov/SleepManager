using System.Linq;
using System.Collections.Generic;
using System;

namespace SleepManager
{
	public interface IDateTimeProvider
	{
		DateTime UtcNow { get; }
	}
}