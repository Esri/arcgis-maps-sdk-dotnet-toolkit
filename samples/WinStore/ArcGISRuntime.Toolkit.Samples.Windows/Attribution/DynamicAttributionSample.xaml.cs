using Esri.ArcGISRuntime.Controls;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;

namespace ArcGISRuntime.Toolkit.Samples.Windows.Attribution
{
	/// <summary>
	/// Demonstrates how to add attribution to the map view that changes based on the maps resolution.
	/// </summary>
	/// <title>Dynamic Attribution</title>
	/// <category>Toolkit</category>
	/// <subcategory>Attribution</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public sealed partial class DynamicAttributionSample : Page
	{
		public DynamicAttributionSample()
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
