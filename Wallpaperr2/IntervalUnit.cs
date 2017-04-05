namespace Wallpaperr2
{
	using System;

	public enum IntervalUnit
	{
		Seconds,
		Minutes,
		Hours,
		Days,
	}

	static class IntervalUnitUtilities
	{
		public static long GetMilliseconds(this IntervalUnit value, double interval)
		{
			switch (value)
			{
				case IntervalUnit.Seconds:
					return TimeSpan.FromSeconds(interval).Milliseconds;

				case IntervalUnit.Minutes:
					return TimeSpan.FromMinutes(interval).Milliseconds;

				case IntervalUnit.Hours:
					return TimeSpan.FromHours(interval).Milliseconds;

				case IntervalUnit.Days:
					return TimeSpan.FromDays(interval).Milliseconds;

				default:
					return TimeSpan.FromMinutes(1).Milliseconds;
			}
		}
	}
}
