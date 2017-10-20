using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples
{
    public class SampleDatasource
    {
        private SampleDatasource() 
        {
            var pages = from t in this.GetType().GetTypeInfo().Assembly.ExportedTypes
                where t.GetTypeInfo().IsSubclassOf(typeof(UserControl)) && t.FullName.Contains(".Samples.")
                select t;

            Samples = (from p in pages select new Sample() { Page = p, Name = p.Name }).ToArray();
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

		public string Category
		{
			get
			{
				int idx = Page.FullName.IndexOf(".Samples.");
				if (idx > 0)
				{
					idx += 9;
					int idx2 = Page.FullName.LastIndexOf(".");
					if(idx2 > idx)
						return Page.FullName.Substring(idx, idx2 - idx);
				}
				return null;
			}
		}
    }
}
