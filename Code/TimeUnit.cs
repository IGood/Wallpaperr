namespace Wallpaperr
{
	using System;

	public class TimeUnit
	{
		#region Static Fields / Properties

		public static readonly TimeUnit[] Units =
		{
			new TimeUnit("seconds", TimeSpan.TicksPerSecond / TimeSpan.TicksPerMillisecond),
			new TimeUnit("minutes", TimeSpan.TicksPerMinute/ TimeSpan.TicksPerMillisecond),
			new TimeUnit("hours", TimeSpan.TicksPerHour/ TimeSpan.TicksPerMillisecond),
			new TimeUnit("days", TimeSpan.TicksPerDay / TimeSpan.TicksPerMillisecond),
		};

		#endregion

		#region Member Fields / Properties

		public string Name { get; }

		public int Value { get; }

		#endregion

		#region Constructors

		private TimeUnit(string name, decimal value)
		{
			this.Name = name;
			this.Value = (int)value;
		}

		#endregion

		public override string ToString() => this.Name;
	}
}
