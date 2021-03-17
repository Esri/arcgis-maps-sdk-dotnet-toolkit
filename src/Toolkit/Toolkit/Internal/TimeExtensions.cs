// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System;
using Esri.ArcGISRuntime.ArcGISServices;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Provides utility extension methods related to time.
    /// </summary>
    internal static class TimeExtensions
    {
        /// <summary>
        /// Adds the specified number of months to the DateTimeOffset object.
        /// </summary>
        /// <param name="startDateTime">The date to add months to.</param>
        /// <param name="months">The number of months to add.</param>
        /// <returns>A DateTimeOffset object with the specified number of months added.</returns>
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

        /// <summary>
        /// Adds the specified TimeValue to the DateTimeOffset object.
        /// </summary>
        /// <param name="startTime">The date to add the time value to.</param>
        /// <param name="timeStep">The amount of time to add.</param>
        /// <returns>A DateTimeOffset object with the specified time added.</returns>
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

            if (timeValueAsTimeSpan.Ticks > 0)
            {
                // Time value maps to a definitive timespan
                return startTime + timeValueAsTimeSpan;
            }
            else
            {
                // Time value is months-based, so can equate to different timespans depending on the particular calendar month(s)
                return startTime.AddMonths(timeValueAsMonths);
            }
        }

        /// <summary>
        /// Divides the specified TimeExtent by the specified number.
        /// </summary>
        /// <param name="timeExtent">The extent to divide.</param>
        /// <param name="count">The amount to divide the extent by.</param>
        /// <returns>A TimeValue instance which, will fit evenly into the input TimeExtent the specified number of times.</returns>
        public static TimeValue Divide(this TimeExtent timeExtent, int count)
        {
            if (timeExtent == null)
            {
                return null;
            }

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
                    if (monthsPerTimeStep % 1200 == 0)
                    {
                        // 1200 months in a century
                        return new TimeValue(monthsPerTimeStep / 1200, TimeUnit.Centuries);
                    }
                    else if (monthsPerTimeStep % 120 == 0)
                    {
                        // 120 months in a decade
                        return new TimeValue(monthsPerTimeStep / 120, TimeUnit.Decades);
                    }
                    else if (monthsPerTimeStep % 12 == 0)
                    {
                        // 12 months in a year
                        return new TimeValue(monthsPerTimeStep / 12, TimeUnit.Years);
                    }
                    else
                    {
                        // largest whole unit the time step interval can be represented in is months
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

            // Check whether milliseconds per time step is a whole number
            if (millisecondsPerTimeStep - Math.Truncate(millisecondsPerTimeStep) == 0)
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

        /// <summary>
        /// Determines whether a specified TimeValue is greater than another TimeValue instance.
        /// </summary>
        /// <returns>A boolean indicating whether the first TimeValue is greater than the second.</returns>
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

        /// <summary>
        /// Converts a TimeValue instance to milliseconds.
        /// </summary>
        /// <param name="timeValue">The TimeValue to convert.</param>
        /// <returns>The TimeValue's equivalent number of milliseconds.</returns>
        /// <remarks>If the time value's unit is months or greater, the return value cannot be determined exactly indepedent of
        /// a known start or end date.  In these cases, a duration of one month is assumed to be (365 / 12) days, and the final
        /// value is determined based on that assumption.</remarks>
        public static double ToMilliseconds(this TimeValue timeValue)
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

        /// <summary>
        /// Merges two TimeExtents together to create one TimeExtent that will encompass both input extents.
        /// </summary>
        /// <param name="timeExtent">The first extent to union.</param>
        /// <param name="otherTimeExtent">The second extent to union.</param>
        /// <returns>A TimeExtent instance with a start time that is the minimum of the that of the two input extents
        /// and an end time that is the maximum of that of the two input extents.</returns>
        public static TimeExtent Union(this TimeExtent timeExtent, TimeExtent otherTimeExtent)
        {
            if (otherTimeExtent == null)
            {
                return timeExtent;
            }

            var startTime = timeExtent.StartTime < otherTimeExtent.StartTime ? timeExtent.StartTime : otherTimeExtent.StartTime;
            var endTime = timeExtent.EndTime > otherTimeExtent.EndTime ? timeExtent.EndTime : otherTimeExtent.EndTime;
            return startTime == endTime ? new TimeExtent(startTime) : new TimeExtent(startTime, endTime);
        }

        /// <summary>
        /// Determines whether the input extent represents instantaneous time.
        /// </summary>
        /// <param name="timeExtent">The time extent to interrogate.</param>
        /// <returns>A boolean indicating whether the extent represents an instant in time.</returns>
        public static bool IsTimeInstant(this TimeExtent timeExtent)
        {
            return timeExtent.StartTime == timeExtent.EndTime;
        }
    }
}