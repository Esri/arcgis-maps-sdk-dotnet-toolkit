using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace Esri.ArcGISRuntime.Toolkit.Samples
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class SampleInfoAttribute : Attribute
    {
        public SampleInfoAttribute()
        {
        }
        public string Category { get; set; }
        public string Description { get; set; }
        public string DisplayName { get; set; }
        public bool ApiKeyRequired { get; set; }
    }

    public class SampleDatasource
    {
        private SampleDatasource() 
        {
            var pages = from t in this.GetType().GetTypeInfo().Assembly.ExportedTypes
                where t.GetTypeInfo().IsSubclassOf(typeof(UserControl)) && t.FullName.Contains(".Samples.")
                select t;

            Samples = (from p in pages select new Sample() { Page = p }).ToArray();
            foreach (var sample in Samples)
            {
                var attr = sample.Page.GetTypeInfo().GetCustomAttribute(typeof(SampleInfoAttribute)) as SampleInfoAttribute;
                if (attr != null)
                {
                    sample.Category = attr.Category;
                    sample.Description = attr.Description;
                    sample.Name = attr.DisplayName;
                    sample.ApiKeyRequired = attr.ApiKeyRequired;
                }
                if (string.IsNullOrEmpty(sample.Name))
                {
                    sample.Name = sample.Page.Name;
                    if (sample.Name.EndsWith("Sample"))
                        sample.Name = sample.Name.Substring(0, sample.Name.Length - 6);
                    sample.Name = Regex.Replace(sample.Name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0").Replace("Arc GIS", "ArcGIS").Replace("Geo View", "GeoView");
                }
                if (string.IsNullOrEmpty(sample.Category))
                {
                    if (!sample.Page.Namespace.EndsWith(".Samples")) //use sub namespace instead
                    {
                        sample.Category = sample.Page.Namespace.Substring(sample.Page.Namespace.IndexOf(".Samples.") + 9);
                    }
                }
            }
        }

        public IEnumerable<Sample> Samples { get; private set; }

        private static SampleDatasource m_Current;
        public static SampleDatasource Current
        {
            get
            {
                if (m_Current == null)
                    m_Current = new SampleDatasource();
                return m_Current;
            }
        }
    }

    public class Sample
    {
        public Type Page { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Category { get; set; }

        public bool ApiKeyRequired { get; set; }
    }
}
