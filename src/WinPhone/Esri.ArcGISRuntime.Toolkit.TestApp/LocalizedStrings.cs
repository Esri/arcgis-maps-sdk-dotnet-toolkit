using Esri.ArcGISRunTime.Toolkit.TestApp.Resources;

namespace Esri.ArcGISRunTime.Toolkit.TestApp
{
	/// <summary>
	/// Provides access to string resources.
	/// </summary>
	public class LocalizedStrings
	{
		private static AppResources _localizedResources = new AppResources();

		public AppResources LocalizedResources { get { return _localizedResources; } }
	}
}