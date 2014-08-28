using ArcGISRuntime.Toolkit.PhoneSampleViewer.Models;
using Windows.UI.Xaml.Controls;

namespace ArcGISRuntime.Toolkit.PhoneSampleViewer
{
	public sealed partial class SamplesPage : Page
	{
		public SamplesPage()
		{
			this.InitializeComponent();
			DataContext = SamplesDataSource.Current;
			NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
		}

		private void ListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var selectedSample = (Sample)e.ClickedItem;
			Frame.Navigate(selectedSample.SampleType);
		}
	}
}
