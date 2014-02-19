using Microsoft.Phone.Controls;
using System;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.TestApp
{
	public partial class MainPage : PhoneApplicationPage
	{
		// Constructor
		public MainPage()
		{
			InitializeComponent();

			// Sample code to localize the ApplicationBar
			//BuildLocalizedApplicationBar();
			DataContext = SampleDatasource.Current;
		}

		private void SampleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var selector = sender as LongListSelector;
			if (selector == null || selector.SelectedItem == null)
				return;
			var pagetype = (selector.SelectedItem as Sample).Page;
			string url = "/Samples/" + pagetype.Name + ".xaml";
			NavigationService.Navigate(new Uri(url, UriKind.Relative));
			selector.SelectedItem = null;
		}
	}
}