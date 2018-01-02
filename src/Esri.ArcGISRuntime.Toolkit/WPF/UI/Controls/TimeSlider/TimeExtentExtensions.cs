using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit
{
    internal static class TimeExtentExtensions
    {
        internal static bool IsTimeInstant(this TimeExtent timeExtent)
        {
            return timeExtent.StartTime == timeExtent.EndTime;
        }
    }
}
