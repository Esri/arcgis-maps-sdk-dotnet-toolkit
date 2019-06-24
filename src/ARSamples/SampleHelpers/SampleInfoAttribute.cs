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
    }
}
