using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.TestApp
{
	public class SampleDatasource
	{
		private SampleDatasource()
		{
			Samples = Application.Current.GetType().GetTypeInfo().Assembly.ExportedTypes
				.Where(t => t.GetTypeInfo().IsSubclassOf(typeof(Page)) && t.FullName.Contains(".Samples."))
				.Select(t => new Sample { Page = t, Name = t.Name }).ToArray();
		}

		public IEnumerable<Sample> Samples { get; private set; }

		private static SampleDatasource _current;
		public static SampleDatasource Current
		{
			get { return _current ?? (_current = new SampleDatasource()); }
		}
	}
	public class Sample
	{
		public Type Page { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }
	}
}
