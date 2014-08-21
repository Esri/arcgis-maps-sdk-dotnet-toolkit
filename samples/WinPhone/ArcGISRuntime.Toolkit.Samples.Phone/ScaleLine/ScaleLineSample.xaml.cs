using Esri.ArcGISRuntime.Controls;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;

namespace ArcGISRuntime.Toolkit.Samples.Phone.ScaleLine
{
	/// <summary>
	/// Demonstrates how to add Scale line to the map view.
	/// </summary>
	/// <title>ScaleLine</title>
	/// <category>Toolkit</category>
	/// <subcategory>ScaleLine</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public sealed partial class ScaleLineSample : Page
	{
		public ScaleLineSample()
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
