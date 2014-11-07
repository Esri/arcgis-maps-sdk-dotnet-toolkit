using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ArcGISRuntime.Toolkit.WindowsSampleViewer
{
    public sealed partial class ViewerPage : Page
    {
		public ViewerPage()
		{
			this.InitializeComponent();
			DataContext = AppState.Current;
			SampleFrame.Navigated += SampleFrame_Navigated;
			SampleFrame.Navigate(typeof(SamplesPage));
		}

		private void SampleFrame_Navigated(object sender, NavigationEventArgs e)
		{
			AppState.Current.CanGoBack = SampleFrame.CanGoBack;
		}

		private void AppBarButton_Click(object sender, RoutedEventArgs e)
		{
			if (SampleFrame.CanGoBack)
			{
				SampleFrame.GoBack();
				AppState.Current.SetDefaultTitle();
			}
		}
    }
}
