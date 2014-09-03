using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Esri.ArcGISRuntime.Security;

namespace Esri.ArcGISRuntime.Toolkit.Security
{
	/// <summary>
	/// WinPhone OAuthAuthorize handler which encapsulates the redirection of the user to the OAuth authorization URI by using a WebView.
	/// </summary>
	public class OAuthAuthorizeHandler : IOAuthAuthorizeHandler
	{
		private string _callbackUrl;
		private TaskCompletionSource<IDictionary<string, string>> _tcs;
		private Popup _popup;

		/// <summary>
		/// Redirects the user to the authorization URI by using a WebView.
		/// </summary>
		/// <param name="serviceUri">The service URI.</param>
		/// <param name="authorizeUri">The authorize URI.</param>
		/// <param name="callbackUri">The callback URI.</param>
		/// <returns>Dictionary of parameters returned by the authorization URI (code, access_token, refresh_token, ...)</returns>
		public async Task<IDictionary<string, string>> AuthorizeAsync(string serviceUri, string authorizeUri, string callbackUri)
		{
			CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
			if (dispatcher == null)
				throw new Exception("No access to UI thread");

			if (_tcs != null || _popup != null)
				throw new Exception(); // only one authorization process at a time

			_callbackUrl = callbackUri;
			_tcs = new TaskCompletionSource<IDictionary<string, string>>();

			// Set an embedded WebView that displays the authorize page
			await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var grid = new Grid();
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
				grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
				grid.Height = Window.Current.Bounds.Height;
				grid.Width = Window.Current.Bounds.Width;

					var webView = new WebView();
				webView.NavigationStarting += WebViewOnNavigationStarting;
				Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtonsOnBackPressed;

				webView.Navigate(new Uri(authorizeUri));
				grid.Children.Add(webView);

				// Display the webView in a popup (default behavior, may be customized by an application)
				_popup = new Popup
				{
					Child = grid,
					IsOpen = true
				};
				_popup.Closed += OnPopupClosed;

			});

			return await _tcs.Task;
		}

		
		// Check if the web view is redirected to the callback url
		void WebViewOnNavigationStarting(WebView webView, WebViewNavigationStartingEventArgs args)
		{
			const string portalApprovalMarker = "/oauth2/approval";
			Uri uri = args.Uri;
			if (webView == null || uri == null || _tcs == null || string.IsNullOrEmpty(uri.AbsoluteUri))
				return;

			bool isRedirected = uri.AbsoluteUri.StartsWith(_callbackUrl) ||
				_callbackUrl.Contains(portalApprovalMarker) && uri.AbsoluteUri.Contains(portalApprovalMarker); // Portal OAuth workflow with org defined at runtime --> the redirect uri can change
			if (isRedirected)
			{
				// The web view is redirected to the callbackUrl ==> close the window, decode the parameters returned as 
				// fragments or query, and return these parameters as result of the Task
				var tcs = _tcs;
				_tcs = null;
				if (_popup != null)
					_popup.IsOpen = false;
				tcs.SetResult(DecodeParameters(uri));
			}
		}

		void OnPopupClosed(object sender, object e)
		{
			Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtonsOnBackPressed;
			if (_tcs != null && !_tcs.Task.IsCompleted)
				_tcs.SetException(new OperationCanceledException()); // user closed the window
			_tcs = null;
			_popup = null;
		}

		private void HardwareButtonsOnBackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
		{
			e.Handled = true;
			if (_popup != null)
				_popup.IsOpen = false;
		}

		/// <summary>
		/// Decodes the parameters returned when the user agent is redirected to the callback url
		/// The parameters can be returned as fragments (e.g. access_token for Browser based app) or as query parameter (e.g. code for Server based app)
		/// </summary>
		/// <param name="uri">The URI.</param>
		private static IDictionary<string, string> DecodeParameters(Uri uri)
		{
			string answer = !string.IsNullOrEmpty(uri.Fragment)
								? uri.Fragment.Substring(1)
								: (!string.IsNullOrEmpty(uri.Query) ? uri.Query.Substring(1) : string.Empty);

			// decode parameters from format key1=value1&key2=value2&...
			return answer.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split('=')).ToDictionary(pair => pair[0], pair => pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : null);
		}
	}
}
