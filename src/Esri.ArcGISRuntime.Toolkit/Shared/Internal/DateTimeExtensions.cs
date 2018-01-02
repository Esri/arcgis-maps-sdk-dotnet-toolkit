using Esri.ArcGISRuntime.ArcGISServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal static class DateTimeExtensions
    {
        internal static DateTimeOffset AddMonths(this DateTimeOffset startDateTime, double months)
        {
            // Get the number of whole and fractional months to add
            var wholeMonths = Math.Truncate(months);
            var partialMonth = months - wholeMonths;

            // Get the date when only whole months are considered
            var newDateTime = startDateTime.AddMonths((int)wholeMonths);

            if (partialMonth > 0)
            {
                // Calculate the number of days to add that correspond to the fractional number of months

                // Get the number of days that would be in the entire next month after the date that results when only whole months are added.
                // We do this because the number of days in the subsequent month can vary between 28 and 31.
                var tempStep = newDateTime.AddMonths(1);
                var daysInNextMonth = (tempStep - newDateTime).TotalDays;

                // Get the number of days from that next month that should be added to the resulting date/time
                var daysInNextStep = partialMonth * daysInNextMonth;

                // Add the days to the whole-months-only date/time
                newDateTime = newDateTime.AddDays(daysInNextStep);
            }

            return newDateTime;
        }

        internal static DateTimeOffset AddTimeValue(this DateTimeOffset startTime, TimeValue timeStep)
        {
            var timeValueAsTimeSpan = TimeSpan.FromMilliseconds(0);
            var timeValueAsMonths = 0d;
            switch (timeStep.Unit)
            {
                case TimeUnit.Milliseconds:
                    timeValueAsTimeSpan = TimeSpan.FromMilliseconds(timeStep.Duration);
                    break;
                case TimeUnit.Seconds:
                    timeValueAsTimeSpan = TimeSpan.FromSeconds(timeStep.Duration);
                    break;
                case TimeUnit.Minutes:
                    timeValueAsTimeSpan = TimeSpan.FromMinutes(timeStep.Duration);
                    break;
                case TimeUnit.Hours:
                    timeValueAsTimeSpan = TimeSpan.FromHours(timeStep.Duration);
                    break;
                case TimeUnit.Days:
                    timeValueAsTimeSpan = TimeSpan.FromDays(timeStep.Duration);
                    break;
                case TimeUnit.Weeks:
                    timeValueAsTimeSpan = TimeSpan.FromDays(timeStep.Duration * 7);
                    break;
                case TimeUnit.Months:
                    timeValueAsMonths = timeStep.Duration;
                    break;
                case TimeUnit.Years:
                    timeValueAsMonths = timeStep.Duration * 12;
                    break;
                case TimeUnit.Decades:
                    timeValueAsMonths = timeStep.Duration * 120;
                    break;
                case TimeUnit.Centuries:
                    timeValueAsMonths = timeStep.Duration * 1200;
                    // Use DateTime.AddMonths for these
                    break;
            }

            if (timeValueAsTimeSpan.Ticks > 0) // Time value maps to a definitive timespan
            {
                return startTime + timeValueAsTimeSpan;
            }
            else // Time value is months-based, so can equate to different timespans depending on the particular calendar month(s)
            {
                return startTime.AddMonths(timeValueAsMonths);
            }
        }
    }
}
