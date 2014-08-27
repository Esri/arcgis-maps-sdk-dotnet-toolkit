using Esri.ArcGISRuntime.Controls;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;

namespace ArcGISRuntime.Toolkit.Samples.Windows.Legend
{
	/// <summary>
	/// Demonstrates how to show legend on the map view.
	/// </summary>
	/// <title>Legend</title>
	/// <category>Toolkit</category>
	/// <subcategory>Legend</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public sealed partial class LegendSample : Page
	{
		public LegendSample()
		{
			this.InitializeComponent();
		}

		private void MyMapView_LayerLoaded(object sender, LayerLoadedEventArgs e)
		{
			if (e.LoadError == null)
				return;

			Debug.WriteLine(string.Format("Error while loading layer : {0} - {1}", e.Layer.ID, e.LoadError.Message));
		}
	}
}
