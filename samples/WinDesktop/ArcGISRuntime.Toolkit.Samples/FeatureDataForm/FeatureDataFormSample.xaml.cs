﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Layers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArcGISRuntime.Toolkit.Samples.Desktop.FeatureDataForm
{
	/// <summary>
	/// Demonstrates how to show use FeatureDataForm to edit Features attributes.
	/// </summary>
	/// <title>Feature data form</title>
	/// <category>Toolkit</category>
	/// <subcategory>FeatureDataForm</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public partial class FeatureDataFormSample : UserControl
	{
		private FeatureLayer _editedLayer;
		private GeodatabaseFeature _editedFeature;

		public FeatureDataFormSample()
		{
			InitializeComponent();
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

					DescriptionTextArea.Visibility = Visibility.Collapsed;
					DataFormArea.Visibility = Visibility.Visible;

					_editedFeature = feature as GeodatabaseFeature;
					_editedLayer = layer;
					_editedLayer.SelectFeatures(new long[] { foundFeatures[0] });

					MyDataForm.GeodatabaseFeature = _editedFeature;
					break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error occured : {0}", ex.ToString()), "Sample error");
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

				DescriptionTextArea.Visibility = Visibility.Visible;
				DataFormArea.Visibility = Visibility.Collapsed;
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error occured : {0}", ex.ToString()), "Sample error");
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			if (MyDataForm.ResetCommand.CanExecute(null))
				MyDataForm.ResetCommand.Execute(null);

			MyDataForm.GeodatabaseFeature = null;

			if (_editedLayer != null)
				_editedLayer.ClearSelection();

			DescriptionTextArea.Visibility = Visibility.Visible;
			DataFormArea.Visibility = Visibility.Collapsed;
		}
	}
}
