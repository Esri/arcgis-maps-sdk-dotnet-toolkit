using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using System.Text.RegularExpressions;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    public class SampleCategoryAttribute : Attribute
    {
        public SampleCategoryAttribute(string name)
        {
            Name = name;
        }
        public string Name { get; set; }
    }

    [Windows.UI.Xaml.Data.Bindable]
	public class SampleDatasource
    {
        private SampleDatasource() 
        {
            var pages = from t in App.Current.GetType().GetTypeInfo().Assembly.ExportedTypes
                where t.GetTypeInfo().IsSubclassOf(typeof(Page)) && t.FullName.Contains(".Samples.")
                select t;

            Samples = (from p in pages select new Sample() { Page = p, Name = p.Name }).ToArray();
			foreach(var sample in Samples)
			{
				var attr = sample.Page.GetTypeInfo().GetCustomAttribute(typeof(SampleCategoryAttribute)) as SampleCategoryAttribute;
				if (attr != null)
					sample.Category = attr.Name;
				else if(!sample.Page.Namespace.EndsWith(".Samples")) //use sub namespace instead
				{
					sample.Category = sample.Page.Namespace.Substring(sample.Page.Namespace.IndexOf(".Samples.") + 9);
				}
				sample.Name = Regex.Replace(sample.Name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
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
		CollectionViewSource m_CollectionViewSource;
		public CollectionViewSource CollectionViewSource
		{
			get
			{
				if(m_CollectionViewSource == null)
				{
					m_CollectionViewSource = new CollectionViewSource()
					{
						IsSourceGrouped = true,
						Source = SamplesByCategory,
						ItemsPath = new Windows.UI.Xaml.PropertyPath("Items")
					};
				}
				return m_CollectionViewSource;
			}
		}
    }
	[Windows.UI.Xaml.Data.Bindable]
	public class SampleGroup
	{
		public SampleGroup(IEnumerable<Sample> samples)
		{
			Items = samples;
		}
		public string Key { get; set; }

		public IEnumerable<Sample> Items { get; private set; }
	}

	[Windows.UI.Xaml.Data.Bindable]
	public class Sample
    {
        public Type Page { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }

		public string Category { get; set; }

        public double FontSize
        {
            get
            {
                if (Order == 0) return 24;
                return 14;
            }
        }
        public int Order { get; set; }
    }
}
