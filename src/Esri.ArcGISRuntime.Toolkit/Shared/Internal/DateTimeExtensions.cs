using Esri.ArcGISRuntime.ArcGISServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal static class DateTimeExtensions
    {
        public static DateTimeOffset AddMonths(this DateTimeOffset startDateTime, double months)
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

        public static DateTimeOffset AddTimeValue(this DateTimeOffset startTime, TimeValue timeStep)
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

        public static TimeValue Divide(this TimeExtent timeExtent, int count)
        {
            if (timeExtent == null)
                return null;

            if (timeExtent.StartTime.TimeOfDay == timeExtent.EndTime.TimeOfDay
                && timeExtent.StartTime.Day == timeExtent.EndTime.Day
                && (timeExtent.StartTime.Month != timeExtent.EndTime.Month
                || timeExtent.StartTime.Year != timeExtent.EndTime.Year))
            {
                // There is a whole number of months between the start and end dates.  Check whether those can be divided evenly
                // by the specified number of time steps.
                var fullExtentSpanInMonths = ((timeExtent.EndTime.Year - timeExtent.StartTime.Year) * 12)
                    + timeExtent.EndTime.Month - timeExtent.StartTime.Month;
                if (fullExtentSpanInMonths % count == 0)
                {
                    // Time steps can be represented as whole months.  Check whether they could also be represented as whole
                    // centuries, decades, or years.

                    var monthsPerTimeStep = fullExtentSpanInMonths / count;
                    if (monthsPerTimeStep % 1200 == 0) // 1200 months in a century
                    {
                        return new TimeValue(monthsPerTimeStep / 1200, TimeUnit.Centuries);
                    }
                    else if (monthsPerTimeStep % 120 == 0) // 120 months in a decade
                    {
                        return new TimeValue(monthsPerTimeStep / 120, TimeUnit.Decades);
                    }
                    else if (monthsPerTimeStep % 12 == 0) // 12 months in a year
                    {
                        return new TimeValue(monthsPerTimeStep / 12, TimeUnit.Years);
                    }
                    else // largest whole unit the time step interval can be represented in is months
                    {
                        return new TimeValue(monthsPerTimeStep, TimeUnit.Months);
                    }
                }
            }

            // The time step interval couldn't be represented as a whole number of months, decades, or centuries.  Check for smaller units.

            const int millisecondsPerSecond = 1000;
            const int millisecondsPerMinute = millisecondsPerSecond * 60;
            const int millisecondsPerHour = millisecondsPerMinute * 60;
            const int millisecondsPerDay = millisecondsPerHour * 24;
            const int millisecondsPerWeek = millisecondsPerDay * 7;

            // Get how many milliseconds would be in each of the specified number of time steps
            var fullExtentTimeSpan = timeExtent.EndTime - timeExtent.StartTime;
            var millisecondsPerTimeStep = fullExtentTimeSpan.TotalMilliseconds / count;

            if (millisecondsPerTimeStep - Math.Truncate(millisecondsPerTimeStep) == 0) // Check whether milliseconds per time step is a whole number
            {
                // Time steps can be represented as whole milliseconds.  Check whether they could also be represented as
                // whole weeks, days, hours, minutes or seconds.

                if (millisecondsPerTimeStep % millisecondsPerWeek == 0)
                {
                    return new TimeValue(millisecondsPerTimeStep / millisecondsPerWeek, TimeUnit.Weeks);
                }
                else if (millisecondsPerTimeStep % millisecondsPerDay == 0)
                {
                    return new TimeValue(millisecondsPerTimeStep / millisecondsPerDay, TimeUnit.Days);
                }
                else if (millisecondsPerTimeStep % millisecondsPerHour == 0)
                {
                    return new TimeValue(millisecondsPerTimeStep / millisecondsPerHour, TimeUnit.Hours);
                }
                else if (millisecondsPerTimeStep % millisecondsPerMinute == 0)
                {
                    return new TimeValue(millisecondsPerTimeStep / millisecondsPerMinute, TimeUnit.Minutes);
                }
                else if (millisecondsPerTimeStep % millisecondsPerSecond == 0)
                {
                    return new TimeValue(millisecondsPerTimeStep / millisecondsPerSecond, TimeUnit.Seconds);
                }
                else
                {
                    return new TimeValue(millisecondsPerTimeStep, TimeUnit.Milliseconds);
                }
            }
            else
            {
                // The full time extent cannot be divided into a non-fractional time step interval.  Fall back to the smallest fractional
                // time step interval with a unit of days or less that is greater than one.  Avoid units of months or greater since the
                // temporal value of a fractional month is dependent on when in the calendar year the value is applied.

                if (millisecondsPerTimeStep / millisecondsPerDay > 1)
                {
                    return new TimeValue(millisecondsPerTimeStep / millisecondsPerDay, TimeUnit.Days);
                }
                else if (millisecondsPerTimeStep / millisecondsPerHour > 1)
                {
                    return new TimeValue(millisecondsPerTimeStep / millisecondsPerHour, TimeUnit.Hours);
                }
                else if (millisecondsPerTimeStep / millisecondsPerMinute > 1)
                {
                    return new TimeValue(millisecondsPerTimeStep / millisecondsPerMinute, TimeUnit.Minutes);
                }
                else if (millisecondsPerTimeStep / millisecondsPerSecond > 1)
                {
                    return new TimeValue(millisecondsPerTimeStep / millisecondsPerSecond, TimeUnit.Seconds);
                }
                else
                {
                    return new TimeValue(millisecondsPerTimeStep, TimeUnit.Milliseconds);
                }
            }
        }

        public static bool IsGreaterThan(this TimeValue timeValue, TimeValue otherTimeValue)
        {
            if (timeValue.Unit == otherTimeValue.Unit)
            {
                return timeValue.Duration > otherTimeValue.Duration;
            }
            else
            {
                return timeValue.ToMilliseconds() > otherTimeValue.ToMilliseconds();
            }
        }

        internal static double ToMilliseconds(this TimeValue timeValue)
        {
            switch (timeValue.Unit)
            {
                case TimeUnit.Centuries:
                    return timeValue.Duration * 86400000 * 36500d;
                case TimeUnit.Decades:
                    return timeValue.Duration * 86400000 * 3650d;
                case TimeUnit.Years:
                    return timeValue.Duration * 86400000 * 365d;
                case TimeUnit.Months:
                    return timeValue.Duration * (365d / 12) * 86400000;
                case TimeUnit.Weeks:
                    return timeValue.Duration * 604800000;
                case TimeUnit.Days:
                    return timeValue.Duration * 86400000;
                case TimeUnit.Hours:
                    return timeValue.Duration * 3600000;
                case TimeUnit.Minutes:
                    return timeValue.Duration * 60000;
                case TimeUnit.Seconds:
                    return timeValue.Duration * 1000;
                case TimeUnit.Milliseconds:
                    return timeValue.Duration;
                default:
                    return timeValue.Duration;
            }
        }

        internal static TimeExtent Union(this TimeExtent timeExtent, TimeExtent otherTimeExtent)
        {
            if (otherTimeExtent == null)
                return timeExtent;

            var startTime = timeExtent.StartTime < otherTimeExtent.StartTime ? timeExtent.StartTime : otherTimeExtent.StartTime;
            var endTime = timeExtent.EndTime > otherTimeExtent.EndTime ? timeExtent.EndTime : otherTimeExtent.EndTime;
            return startTime == endTime ? new TimeExtent(startTime) : new TimeExtent(startTime, endTime);
        }
    }
}
