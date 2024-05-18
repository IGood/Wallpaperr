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
        public static TimeSpan ToTimeSpan(this int value, IntervalUnit units)
        {
            switch (units)
            {
                case IntervalUnit.Seconds:
                    return TimeSpan.FromSeconds(value);

                case IntervalUnit.Minutes:
                    return TimeSpan.FromMinutes(value);

                case IntervalUnit.Hours:
                    return TimeSpan.FromHours(value);

                case IntervalUnit.Days:
                    return TimeSpan.FromDays(value);

                default:
                    return TimeSpan.FromMinutes(1);
            }
        }
    }
}
