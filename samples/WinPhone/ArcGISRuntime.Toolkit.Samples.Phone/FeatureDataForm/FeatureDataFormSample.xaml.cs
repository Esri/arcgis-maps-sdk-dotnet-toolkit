using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Layers;
using System;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ArcGISRuntime.Toolkit.Samples.Phone.FeatureDataForm
{
	/// <summary>
	/// Demonstrates how to show use FeatureDataForm to edit Features attributes.
	/// </summary>
	/// <title>Feature data form</title>
	/// <category>Toolkit</category>
	/// <subcategory>FeatureDataForm</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public sealed partial class FeatureDataFormSample : Page
	{
		private FeatureLayer _editedLayer;
		private GeodatabaseFeature _editedFeature;

		private CommandBar _dataFormCommandBar;
		private bool _showDataFormCommands;

		public FeatureDataFormSample()
		{
			this.InitializeComponent();
			CreateAppBar();
		}

		private async void MyMapView_MapViewTapped(object sender, Esri.ArcGISRuntime.Controls.MapViewInputEventArgs e)
		{
			try
			{
				if (MyDataForm.ResetCommand.CanExecute(null))
					MyDataForm.ResetCommand.Execute(null);

				MyDataForm.GeodatabaseFeature = null;

				if (_editedLayer != null)
					_editedLayer.ClearSelection();

				foreach (var layer in MyMapView.Map.Layers.OfType<FeatureLayer>().Reverse())
				{
					// Get possible features and if none found, move to next layer
					var foundFeatures = await layer.HitTestAsync(MyMapView, new Rect(e.Position, new Size(10, 10)), 1);
					if (foundFeatures.Count() == 0)
						continue;

					// Get feature from table
					var feature = await layer.FeatureTable.QueryAsync(foundFeatures[0]);

					// Change UI
					DataFormView.Visibility = Visibility.Visible;
					ToggleAppBar();

					_editedFeature = feature as GeodatabaseFeature;
					_editedLayer = layer;
					_editedLayer.SelectFeatures(new long[] { foundFeatures[0] });

					// Set feature that is being edited to data form
					MyDataForm.GeodatabaseFeature = _editedFeature;
					return;
				}
			}
			catch (Exception ex)
			{
				var _x = new MessageDialog(string.Format("Error occured : {0}", ex.ToString()), "Sample error").ShowAsync();
			}
		}

		private async void MyDataForm_ApplyCompleted(object sender, EventArgs e)
		{
			try
			{
				if (_editedLayer == null || _editedFeature == null)
					return;

				// Commit changes to the local cache
				await _editedLayer.FeatureTable.UpdateAsync(_editedFeature);

				// To commit changed to the service use ApplyEdits
				// await (_editedLayer.FeatureTable as ServiceFeatureTable).ApplyEditsAsync();

				_editedLayer.ClearSelection();

				DataFormView.Visibility = Visibility.Collapsed;
				ToggleAppBar();
			}
			catch (Exception ex)
			{
				var _x = new MessageDialog(string.Format("Error occured : {0}", ex.ToString()), "Sample error").ShowAsync();
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			if (MyDataForm.ResetCommand.CanExecute(null))
				MyDataForm.ResetCommand.Execute(null);

			MyDataForm.GeodatabaseFeature = null;

			if (_editedLayer != null)
				_editedLayer.ClearSelection();


			DataFormView.Visibility = Visibility.Collapsed;
			ToggleAppBar();
		}

		private void MyMapView_LayerLoaded(object sender, LayerLoadedEventArgs e)
		{
			if (e.LoadError == null)
				return;

			Debug.WriteLine(string.Format("Error while loading layer : {0} - {1}", e.Layer.ID, e.LoadError.Message));
		}

		private void CreateAppBar()
		{
			_dataFormCommandBar = new CommandBar();

			AppBarButton applyButton = new AppBarButton();
			applyButton.Icon = new SymbolIcon(Symbol.Save);
			applyButton.Label = "Apply";
			applyButton.Command = MyDataForm.ApplyCommand;

			_dataFormCommandBar.PrimaryCommands.Add(applyButton);

			AppBarButton cancelButton = new AppBarButton();
			cancelButton.Icon = new SymbolIcon(Symbol.Cancel);
			cancelButton.Label = "Cancel";
			cancelButton.Click += Cancel_Click;

			_dataFormCommandBar.PrimaryCommands.Add(cancelButton);
		}

		private void ToggleAppBar()
		{
			_showDataFormCommands = !_showDataFormCommands;
			
			if (_showDataFormCommands)
				BottomAppBar = _dataFormCommandBar;
			else
				BottomAppBar = null;
		}
	}
}
