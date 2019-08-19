using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class SampleInfoAttributeAttribute : Attribute
    {
        public SampleInfoAttributeAttribute()
        {
        }
        public string Category { get; set; }
        public string Description { get; set; }
        public string DisplayName { get; set; }
    }

	public class SampleDatasource
    {
        private SampleDatasource() 
        {
            var pages = from t in typeof(AppDelegate).GetTypeInfo().Assembly.ExportedTypes
                where t.GetTypeInfo().IsSubclassOf(typeof(UIViewController)) && t.FullName.Contains(".Samples.")
                select t;

            Samples = (from p in pages select new Sample() { Page = p }).ToArray();
			foreach(var sample in Samples)
			{
				var attr = sample.Page.GetTypeInfo().GetCustomAttribute(typeof(SampleInfoAttributeAttribute)) as SampleInfoAttributeAttribute;
                if (attr != null)
                {
                    sample.Category = attr.Category;
                    sample.Description = attr.Description;
                    sample.Name = attr.DisplayName;
                }
                if (string.IsNullOrEmpty(sample.Name))
                {
                    sample.Name = sample.Page.Name;
                    if (sample.Name.EndsWith("ViewController"))
                        sample.Name = sample.Name.Substring(0, sample.Name.Length - 14);
                    sample.Name = Regex.Replace(sample.Name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0").Replace("Arc GIS", "ArcGIS");
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

		public IEnumerable<SampleGroup> SamplesByCategory
		{
			get
			{
				var groups = (from item in Samples
							 orderby item.Category
							 group item by item.Category into g
							 select new SampleGroup(g) { Key = g.Key ?? "Misc" });
				return groups;
			}
		}
    }

	public class SampleGroup
	{
		public SampleGroup(IEnumerable<Sample> samples)
		{
			Items = samples;
		}
		public string Key { get; set; }

		public IEnumerable<Sample> Items { get; private set; }
	}

	public class Sample
    {
        public Type Page { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }

		public string Category { get; set; }
    }
}
