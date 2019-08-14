using System;
using System.Collections.Generic;
using System.Text;

namespace ARToolkit.SampleApp
{
    public class SampleInfoAttribute : Attribute
    {
        public SampleInfoAttribute()
        {
        }
        public string Category { get; set; }
        public string Description { get; set; }
        public string DisplayName { get; set; }
        public Esri.ArcGISRuntime.ARToolkit.DeviceSupport DeviceRequirement { get; set; } = Esri.ArcGISRuntime.ARToolkit.DeviceSupport.SixDegreesOfFreedom;
    }
}
