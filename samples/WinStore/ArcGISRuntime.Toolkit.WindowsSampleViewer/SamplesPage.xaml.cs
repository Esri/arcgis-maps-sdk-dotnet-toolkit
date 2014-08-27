using ArcGISRuntime.Toolkit.WindowsSampleViewer.Models;
using Windows.UI.Xaml.Controls;

namespace ArcGISRuntime.Toolkit.WindowsSampleViewer
{
	public sealed partial class SamplesPage : Page
	{
		public SamplesPage()
		{
			this.InitializeComponent();
			DataContext = SamplesDataSource.Current;
		}

		private void GridView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var selectedSample = (Sample)e.ClickedItem;
			Frame.Navigate(selectedSample.SampleType);
			AppState.Current.CurrentSampleTitle = selectedSample.Name;
		}
	}
}
