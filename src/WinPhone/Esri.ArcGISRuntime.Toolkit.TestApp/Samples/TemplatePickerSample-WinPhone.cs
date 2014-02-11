// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Layers;
using System;
using System.Windows;
using Esri.ArcGISRuntime.Toolkit.Controls;

namespace Esri.ArcGISRuntime.Toolkit.TestApp.Samples
{
	/// <summary>
	/// Interaction logic for TemplatePickerSample.xaml
	/// </summary>
	public partial class TemplatePickerSample
	{
		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			if (LayersPopup.IsOpen)
			{
				LayersPopup.IsOpen = false;
				e.Cancel = true;
			}
			base.OnNavigatingFrom(e);
		}

		private void TooggleLayers(object sender, EventArgs e)
		{
			LayersPopup.IsOpen = !LayersPopup.IsOpen;
		}

		private async void InitializeFeatureServicesOnLoaded(object sender, RoutedEventArgs e)
		{
			// Initialize 
			var templatePicker = sender as TemplatePicker;
			foreach (var flayer in templatePicker.Layers.OfType<FeatureLayer>().ToArray())
			{
				try
				{
					await ((GeodatabaseFeatureServiceTable)flayer.FeatureTable).InitializeAsync();
				}
				catch{}
			}
			ProgressBar.Visibility = Visibility.Collapsed;
		}

		private void LayersPopup_OnLoaded(object sender, RoutedEventArgs e)
		{
			var popup = sender as Popup;
			if (popup != null)
			{
				popup.Height = Application.Current.Host.Content.ActualHeight;
				popup.Width = Application.Current.Host.Content.ActualWidth;
			}
		}
	}
}
